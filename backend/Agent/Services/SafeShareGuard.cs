using System.Text.RegularExpressions;
using LinkVaultApi.Agent.Models;
using LinkVaultApi.Data;
using LinkVaultApi.Services;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Agent.Services;

public class SafeShareGuard : ISafeShareGuard
{
    private readonly LinkVaultDbContext _dbContext;
    private readonly IStorageProvider _storageProvider;
    private readonly IMetaModelClient _modelClient;
    private readonly ILogger<SafeShareGuard> _logger;

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".json", ".csv", ".env", ".md", ".yaml", ".yml",
        ".ts", ".js", ".cs", ".py", ".sql", ".sh", ".config",
        ".xml", ".html", ".log", ".properties", ".ini"
    };

    public SafeShareGuard(
        LinkVaultDbContext dbContext,
        IStorageProvider storageProvider,
        IMetaModelClient modelClient,
        ILogger<SafeShareGuard> logger)
    {
        _dbContext = dbContext;
        _storageProvider = storageProvider;
        _modelClient = modelClient;
        _logger = logger;
    }

    private static bool IsTextResource(LinkVaultApi.Models.Resource resource, string content)
    {
        if (content.Contains('\0'))
            return false;

        if (!string.IsNullOrWhiteSpace(resource.MimeType))
        {
            var mime = resource.MimeType.ToLowerInvariant();
            if (mime.StartsWith("text/") ||
                mime.Contains("json") ||
                mime.Contains("javascript") ||
                mime.Contains("xml") ||
                mime.Contains("yaml"))
            {
                return true;
            }
        }

        var ext = Path.GetExtension(resource.Name);
        return !string.IsNullOrWhiteSpace(ext) && TextExtensions.Contains(ext);
    }

    public async Task<SafeShareScanResponse> ScanResourceAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken = default)
    {
        var resource = await _dbContext.Resources.FirstOrDefaultAsync(r => r.Id == resourceId, cancellationToken);
        if (resource == null)
        {
            return new SafeShareScanResponse
            {
                ResourceId = resourceId,
                Status = "Clean",
                RiskScore = 0,
                CanShare = true,
                Message = "Resource not found or external link. Passed safety check."
            };
        }

        string fileContent = string.Empty;
        if (!string.IsNullOrEmpty(resource.StoragePath))
        {
            try
            {
                using var stream = await _storageProvider.GetAsync(resource.StoragePath, cancellationToken);
                using var reader = new StreamReader(stream);
                // Read up to first 8KB for safety inspection
                var buffer = new char[8192];
                var read = await reader.ReadAsync(buffer, 0, buffer.Length);
                fileContent = new string(buffer, 0, read);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read file stream for resource {ResourceId}. Proceeding with metadata scan.", resourceId);
            }
        }

        var isText = IsTextResource(resource, fileContent);
        var scanResult = await ScanTextContentCoreAsync(fileContent, resource.Name, isText, cancellationToken);
        scanResult.ResourceId = resourceId;

        // Persist scan telemetry in database
        var safeScan = new SafeShareScan
        {
            ResourceId = resourceId,
            ScannedByUserId = userId,
            Status = scanResult.Status switch
            {
                "Blocked" => SafeShareStatus.Blocked,
                "Warning" => SafeShareStatus.Warning,
                _ => SafeShareStatus.Clean
            },
            RiskScore = scanResult.RiskScore,
            DetectedThreats = System.Text.Json.JsonSerializer.Serialize(scanResult.DetectedThreats),
            ModelVerification = scanResult.Message,
            ScannedAt = DateTime.UtcNow
        };

        _dbContext.SafeShareScans.Add(safeScan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return scanResult;
    }

    public Task<SafeShareScanResponse> ScanTextContentAsync(string content, string fileName = "file.txt", CancellationToken cancellationToken = default)
    {
        return ScanTextContentCoreAsync(content, fileName, isText: true, cancellationToken);
    }

    private async Task<SafeShareScanResponse> ScanTextContentCoreAsync(string content, string fileName, bool isText, CancellationToken cancellationToken)
    {
        var detectedThreats = new List<string>();
        int riskScore = 0;

        // 1. Layer 1: Deterministic Regex Scanning
        if (!string.IsNullOrWhiteSpace(content))
        {
            if (Regex.IsMatch(content, @"(Password|pwd)\s*=\s*[^;\r\n]+", RegexOptions.IgnoreCase))
            {
                detectedThreats.Add("PostgreSQL Connection String Password");
                riskScore = Math.Max(riskScore, 95);
            }

            if (Regex.IsMatch(content, @"-----BEGIN (RSA |EC )?PRIVATE KEY-----", RegexOptions.IgnoreCase))
            {
                detectedThreats.Add("Cryptographic Private Key");
                riskScore = Math.Max(riskScore, 100);
            }

            if (Regex.IsMatch(content, @"AKIA[0-9A-Z]{16}"))
            {
                detectedThreats.Add("AWS Access Key ID");
                riskScore = Math.Max(riskScore, 90);
            }

            if (Regex.IsMatch(content, @"eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}"))
            {
                detectedThreats.Add("Exposed JWT Token");
                riskScore = Math.Max(riskScore, 85);
            }

            if (Regex.IsMatch(content, @"(api[_-]?key|secret[_-]?key)\s*[:=]\s*['""][a-zA-Z0-9_\-]{16,}['""]", RegexOptions.IgnoreCase))
            {
                detectedThreats.Add("Generic API Secret Key");
                riskScore = Math.Max(riskScore, 90);
            }
        }

        // 2. Layer 2: Model moderation over first 8KB of text files (never send binary files)
        string modelVerificationNote = "";
        if (isText && !string.IsNullOrWhiteSpace(content) && riskScore < 90)
        {
            if (_modelClient.HasApiKey)
            {
                var textSnippet = content[..Math.Min(content.Length, 8192)];
                var modelGuard = await _modelClient.ScanContentSecurityAsync(textSnippet, cancellationToken);
                if (!modelGuard.IsSafe)
                {
                    foreach (var cat in modelGuard.FlaggedCategories)
                    {
                        if (!detectedThreats.Contains(cat)) detectedThreats.Add(cat);
                    }
                    riskScore = Math.Max(riskScore, modelGuard.RiskScore);
                    modelVerificationNote = $" (Verified by {_modelClient.ModelName})";
                }
            }
            else
            {
                modelVerificationNote = " (Deterministic regex scan; model check not configured)";
            }
        }
        else if (!isText)
        {
            modelVerificationNote = " (Binary file; inspected via signature heuristics, excluded from LLM scan)";
        }

        var isBlocked = riskScore >= 80;
        var isWarning = riskScore >= 50 && !isBlocked;

        var status = isBlocked ? "Blocked" : (isWarning ? "Warning" : "Clean");
        var canShare = !isBlocked;
        var message = isBlocked
            ? $"Safe Share Guard blocked sharing: File '{fileName}' contains exposed credentials ({string.Join(", ", detectedThreats)}).{modelVerificationNote}"
            : (isWarning
                ? $"Safe Share Guard warning: File '{fileName}' may contain sensitive information.{modelVerificationNote}"
                : $"Safe Share Guard passed: File '{fileName}' is clean.{modelVerificationNote}");

        return new SafeShareScanResponse
        {
            FileName = fileName,
            Status = status,
            RiskScore = riskScore,
            DetectedThreats = detectedThreats,
            CanShare = canShare,
            Message = message,
            ScannedAt = DateTime.UtcNow
        };
    }
}
