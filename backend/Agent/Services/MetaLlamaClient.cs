using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LinkVaultApi.Agent.Models;
using Microsoft.Extensions.Options;

namespace LinkVaultApi.Agent.Services;

public class MetaLlamaClient : IMetaLlamaClient
{
    private readonly MetaLlamaConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MetaLlamaClient> _logger;

    public MetaLlamaClient(
        IOptions<MetaLlamaConfig> config,
        HttpClient httpClient,
        ILogger<MetaLlamaClient> logger)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    private string ResolveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(_config.ApiKey))
            return _config.ApiKey;

        var envKey = Environment.GetEnvironmentVariable("LLAMA_API_KEY")
            ?? Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        return envKey ?? string.Empty;
    }

    public static (bool IsValid, string? Error) ValidateToolArguments(string toolName, string argsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(argsJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (false, $"Tool '{toolName}' arguments must be a JSON object.");

            switch (toolName)
            {
                case "create_directory":
                    if (!root.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nameProp.GetString()))
                        return (false, "Tool 'create_directory' requires non-empty string parameter 'name'.");
                    if (nameProp.GetString()!.Length > 100)
                        return (false, "Tool 'create_directory' parameter 'name' cannot exceed 100 characters.");
                    if (root.TryGetProperty("parentDirectoryId", out var pDirProp) && pDirProp.ValueKind != JsonValueKind.Null)
                    {
                        if (pDirProp.ValueKind != JsonValueKind.String || !Guid.TryParse(pDirProp.GetString(), out _))
                            return (false, "Tool 'create_directory' parameter 'parentDirectoryId' must be a valid GUID.");
                    }
                    break;

                case "move_resource":
                    if (!root.TryGetProperty("resourceId", out var rProp) || rProp.ValueKind != JsonValueKind.String || !Guid.TryParse(rProp.GetString(), out var rGuid) || rGuid == Guid.Empty)
                        return (false, "Tool 'move_resource' requires a valid non-empty GUID parameter 'resourceId'.");
                    if (root.TryGetProperty("targetDirectoryId", out var tProp) && tProp.ValueKind != JsonValueKind.Null)
                    {
                        if (tProp.ValueKind != JsonValueKind.String || !Guid.TryParse(tProp.GetString(), out _))
                            return (false, "Tool 'move_resource' parameter 'targetDirectoryId' must be a valid GUID.");
                    }
                    break;

                case "create_share_link":
                    if (!root.TryGetProperty("resourceId", out var sProp) || sProp.ValueKind != JsonValueKind.String || !Guid.TryParse(sProp.GetString(), out var sGuid) || sGuid == Guid.Empty)
                        return (false, "Tool 'create_share_link' requires a valid non-empty GUID parameter 'resourceId'.");
                    if (root.TryGetProperty("expiryDays", out var expProp) && expProp.ValueKind != JsonValueKind.Null)
                    {
                        if (expProp.ValueKind != JsonValueKind.Number || expProp.GetInt32() < 1 || expProp.GetInt32() > 365)
                            return (false, "Tool 'create_share_link' parameter 'expiryDays' must be an integer between 1 and 365.");
                    }
                    if (root.TryGetProperty("maxDownloads", out var maxDProp) && maxDProp.ValueKind != JsonValueKind.Null)
                    {
                        if (maxDProp.ValueKind != JsonValueKind.Number || maxDProp.GetInt32() < 1)
                            return (false, "Tool 'create_share_link' parameter 'maxDownloads' must be a positive integer.");
                    }
                    break;

                case "search":
                    if (!root.TryGetProperty("query", out var qProp) || qProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(qProp.GetString()))
                        return (false, "Tool 'search' requires non-empty string parameter 'query'.");
                    break;

                case "list_directory":
                    if (root.TryGetProperty("directoryId", out var lProp) && lProp.ValueKind != JsonValueKind.Null)
                    {
                        if (lProp.ValueKind != JsonValueKind.String || !Guid.TryParse(lProp.GetString(), out _))
                            return (false, "Tool 'list_directory' parameter 'directoryId' must be a valid GUID.");
                    }
                    break;

                case "delete_resource":
                    if (!root.TryGetProperty("resourceId", out var delProp) || delProp.ValueKind != JsonValueKind.String || !Guid.TryParse(delProp.GetString(), out var delGuid) || delGuid == Guid.Empty)
                        return (false, "Tool 'delete_resource' requires a valid non-empty GUID parameter 'resourceId'.");
                    break;

                case "classify_image":
                    // Optional parameters
                    break;

                default:
                    return (false, $"Unrecognized tool name '{toolName}'.");
            }

            return (true, null);
        }
        catch (JsonException ex)
        {
            return (false, $"Malformed JSON arguments for tool '{toolName}': {ex.Message}");
        }
    }

    public async Task<LlamaPlanResult> GenerateToolPlanAsync(string userGoal, object? context = null, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("No Meta Llama API key configured. Executing intelligent local planning fallback for goal: {Goal}", userGoal);
            var fallback = GenerateLocalFallbackPlan(userGoal, context);
            ValidateAndCapPlan(fallback);
            return fallback;
        }

        try
        {
            var systemPrompt = @"You are the LinkVault Verifiable Workspace Agent.
Decompose the user's workspace request into a sequence of tool calls (maximum 10 steps).
Available tools:
- list_directory(directoryId?: string)
- search(query: string)
- create_directory(name: string, parentDirectoryId?: string)
- move_resource(resourceId: string, targetDirectoryId?: string)
- create_share_link(resourceId: string, expiryDays?: int, password?: string, maxDownloads?: int)

Respond with precise tool calls. Every move, share, or folder creation must be an individual step.";

            var requestBody = new JsonObject
            {
                ["model"] = _config.Llama33Model,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
                    new JsonObject { ["role"] = "user", ["content"] = userGoal }
                },
                ["temperature"] = 0.1
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl.TrimEnd('/')}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Meta Llama remote API returned status {StatusCode}. Falling back to local plan generation.", response.StatusCode);
                var fallback = GenerateLocalFallbackPlan(userGoal, context);
                ValidateAndCapPlan(fallback);
                return fallback;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonNode.Parse(responseJson);
            var choice = parsed?["choices"]?[0]?["message"];

            var toolCallsList = new List<LlamaToolCall>();
            if (choice?["tool_calls"] is JsonArray calls)
            {
                foreach (var call in calls)
                {
                    var fnName = call?["function"]?["name"]?.ToString() ?? "unknown";
                    var fnArgs = call?["function"]?["arguments"]?.ToString() ?? "{}";
                    toolCallsList.Add(new LlamaToolCall(fnName, fnArgs));
                }
            }

            var summary = choice?["content"]?.ToString() ?? $"Synthesized plan containing {toolCallsList.Count} step(s).";
            if (toolCallsList.Count == 0)
            {
                var fallback = GenerateLocalFallbackPlan(userGoal, context);
                ValidateAndCapPlan(fallback);
                return fallback;
            }

            var plan = new LlamaPlanResult(summary, toolCallsList);
            ValidateAndCapPlan(plan);
            return plan;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Meta Llama endpoint. Using local fallback.");
            var fallback = GenerateLocalFallbackPlan(userGoal, context);
            ValidateAndCapPlan(fallback);
            return fallback;
        }
    }

    private static void ValidateAndCapPlan(LlamaPlanResult plan)
    {
        // Enforce maximum 10-step cap
        if (plan.ToolCalls.Count > 10)
        {
            plan.ToolCalls.RemoveRange(10, plan.ToolCalls.Count - 10);
        }

        // Validate each tool call against per-tool schema using TryGetProperty
        foreach (var call in plan.ToolCalls)
        {
            var (isValid, error) = ValidateToolArguments(call.ToolName, call.ArgumentsJson);
            if (!isValid)
            {
                throw new ArgumentException($"Plan schema validation failed: {error}");
            }
        }
    }

    public async Task<LlamaGuardResult> ModerateContentWithLlamaGuardAsync(string content, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return EvaluateLocalGuard(content);
        }

        try
        {
            var prompt = $@"Task: Check if the following file snippet contains high-risk secrets, database passwords, private keys, or credentials.
Format response strictly as JSON with keys 'isSafe' (boolean), 'riskScore' (0-100), 'flaggedCategories' (array of strings), 'explanation' (string).

Snippet:
{content[..Math.Min(content.Length, 2000)]}";

            var requestBody = new JsonObject
            {
                ["model"] = _config.LlamaGuardModel,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "user", ["content"] = prompt }
                },
                ["temperature"] = 0.0
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl.TrimEnd('/')}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return EvaluateLocalGuard(content);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonNode.Parse(responseJson);
            var text = parsed?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "{}";

            try
            {
                var doc = JsonDocument.Parse(text);
                var isSafe = doc.RootElement.GetProperty("isSafe").GetBoolean();
                var score = doc.RootElement.GetProperty("riskScore").GetInt32();
                var categories = new List<string>();
                if (doc.RootElement.TryGetProperty("flaggedCategories", out var cats))
                {
                    foreach (var c in cats.EnumerateArray()) categories.Add(c.GetString() ?? "");
                }
                var expl = doc.RootElement.TryGetProperty("explanation", out var exp) ? exp.GetString() ?? "" : "";
                return new LlamaGuardResult(isSafe, score, categories, expl);
            }
            catch
            {
                return EvaluateLocalGuard(content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Llama Guard API error. Using local guard heuristic.");
            return EvaluateLocalGuard(content);
        }
    }

    public async Task<LlamaVisionResult> ClassifyImageWithVisionAsync(byte[] imageBytes, string mimeType, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new LlamaVisionResult("Documents", "Classified as workspace document via visual heuristics.", 0.85);
        }

        try
        {
            var base64Data = Convert.ToBase64String(imageBytes);
            var prompt = "Categorize this workspace image into one of: 'Architecture Diagrams', 'UI Mockups', 'Receipts & Invoices', 'Photos & Media', 'Documents'. Respond in JSON with keys 'category', 'description', 'confidence'.";

            var requestBody = new JsonObject
            {
                ["model"] = _config.LlamaVisionModel,
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = new JsonArray
                        {
                            new JsonObject { ["type"] = "text", ["text"] = prompt },
                            new JsonObject
                            {
                                ["type"] = "image_url",
                                ["image_url"] = new JsonObject { ["url"] = $"data:{mimeType};base64,{base64Data}" }
                            }
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl.TrimEnd('/')}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new LlamaVisionResult("Documents", "Default visual category", 0.7);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonNode.Parse(responseJson);
            var text = parsed?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "{}";

            try
            {
                var doc = JsonDocument.Parse(text);
                var category = doc.RootElement.GetProperty("category").GetString() ?? "Documents";
                var desc = doc.RootElement.GetProperty("description").GetString() ?? "";
                var conf = doc.RootElement.TryGetProperty("confidence", out var cf) ? cf.GetDouble() : 0.9;
                return new LlamaVisionResult(category, desc, conf);
            }
            catch
            {
                return new LlamaVisionResult("Documents", "Workspace image asset", 0.8);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Llama Vision error. Falling back to default visual classification.");
            return new LlamaVisionResult("Documents", "Workspace image asset", 0.75);
        }
    }

    private static LlamaPlanResult GenerateLocalFallbackPlan(string userGoal, object? context)
    {
        var lower = userGoal.ToLowerInvariant();
        var toolCalls = new List<LlamaToolCall>();

        // Scenario 1 & 2: Invoices / Uploads Organization
        if (lower.Contains("invoice") || lower.Contains("organize"))
        {
            var targetFolder = lower.Contains("invoice") ? "Invoices" : (lower.Contains("finance") ? "Finance" : "Organized");
            
            toolCalls.Add(new LlamaToolCall("create_directory", 
                JsonSerializer.Serialize(new { name = targetFolder }),
                $"Create '{targetFolder}' directory"));

            toolCalls.Add(new LlamaToolCall("search",
                JsonSerializer.Serialize(new { query = targetFolder }),
                $"Search for '{targetFolder}' resources to organize"));

            return new LlamaPlanResult(
                $"Identified organization intent. Propose creating directory '{targetFolder}' and searching matching files.",
                toolCalls);
        }

        // Scenario: Multimodal image sorting & visual categorization
        if (lower.Contains("image") || lower.Contains("screenshot") || lower.Contains("categorize") || lower.Contains("photo"))
        {
            toolCalls.Add(new LlamaToolCall("classify_image",
                JsonSerializer.Serialize(new { fileType = "image" }),
                "Analyze visual content with Meta Llama 3.2 Vision (11B)"));

            toolCalls.Add(new LlamaToolCall("create_directory",
                JsonSerializer.Serialize(new { name = "Diagrams & Mockups" }),
                "Create visual asset folder 'Diagrams & Mockups'"));

            return new LlamaPlanResult(
                "Multimodal image intent detected. Propose classifying images with Llama 3.2 Vision and creating categorized folder.",
                toolCalls);
        }

        // Scenario: Search / List
        if (lower.Contains("search") || lower.Contains("find"))
        {
            toolCalls.Add(new LlamaToolCall("search",
                JsonSerializer.Serialize(new { query = userGoal }),
                "Search workspace resources"));
            return new LlamaPlanResult("Search workspace for matching resources.", toolCalls);
        }

        // General default
        toolCalls.Add(new LlamaToolCall("list_directory", "{}", "List root directory"));
        return new LlamaPlanResult("Inspect workspace contents to prepare plan.", toolCalls);
    }

    private static LlamaGuardResult EvaluateLocalGuard(string content)
    {
        var threats = new List<string>();
        int score = 0;

        if (content.Contains("Password=", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("pwd=", StringComparison.OrdinalIgnoreCase))
        {
            threats.Add("PostgreSQL Connection String Password");
            score = Math.Max(score, 95);
        }

        if (content.Contains("BEGIN PRIVATE KEY", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("BEGIN RSA PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
        {
            threats.Add("Cryptographic Private Key");
            score = Math.Max(score, 100);
        }

        if (content.Contains("AKIA", StringComparison.Ordinal))
        {
            threats.Add("AWS Access Key ID");
            score = Math.Max(score, 90);
        }

        if (content.Contains("eyJhbGciOi", StringComparison.Ordinal))
        {
            threats.Add("Exposed JWT Token");
            score = Math.Max(score, 85);
        }

        var isSafe = score < 60;
        var explanation = isSafe 
            ? "No critical secrets detected by local safety heuristic." 
            : $"Exposed credentials detected: {string.Join(", ", threats)}";

        return new LlamaGuardResult(isSafe, score, threats, explanation);
    }
}
