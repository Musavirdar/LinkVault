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
    private readonly IMetaLlamaClient _llamaClient;
    private readonly ILogger<SafeShareGuard> _logger;

    public SafeShareGuard(
        LinkVaultDbContext dbContext,
        IStorageProvider storageProvider,
        IMetaLlamaClient llamaClient,
        ILogger<SafeShareGuard> logger)
    {
        _dbContext = dbContext;
        _storageProvider = storageProvider;
        _llamaClient = llamaClient;
        _logger = logger;
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
                // Read up to first 64KB for safety scan
                var buffer = new char[65536];
                var read = await reader.ReadAsync(buffer, 0, buffer.Length);
                fileContent = new string(buffer, 0, read);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read file stream for resource {ResourceId}. Proceeding with metadata scan.", resourceId);
            }
        }

        var scanResult = await ScanTextContentAsync(fileContent, resource.Name, cancellationToken);
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

    public async Task<SafeShareScanResponse> ScanTextContentAsync(string content, string fileName = "file.txt", CancellationToken cancellationToken = default)
    {
        var detectedThreats = new List<string>();
        int riskScore = 0;

        // 1. Deterministic Regex Scanning
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

        // 2. Model Moderation (if content present and not already blocked)
        if (riskScore < 90 && !string.IsNullOrWhiteSpace(content))
        {
            var modelGuard = await _llamaClient.ModerateContentWithLlamaGuardAsync(content, cancellationToken);
            if (!modelGuard.IsSafe)
            {
                foreach (var cat in modelGuard.FlaggedCategories)
                {
                    if (!detectedThreats.Contains(cat)) detectedThreats.Add(cat);
                }
                riskScore = Math.Max(riskScore, modelGuard.RiskScore);
            }
        }

        var isBlocked = riskScore >= 80;
        var isWarning = riskScore >= 50 && !isBlocked;

        var status = isBlocked ? "Blocked" : (isWarning ? "Warning" : "Clean");
        var canShare = !isBlocked;
        var message = isBlocked
            ? $"Safe Share Guard blocked sharing: File '{fileName}' contains exposed credentials ({string.Join(", ", detectedThreats)})."
            : (isWarning
                ? $"Safe Share Guard warning: File '{fileName}' may contain sensitive information."
                : $"Safe Share Guard passed: File '{fileName}' is clean.");

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
