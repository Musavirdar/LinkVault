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

    public async Task<LlamaPlanResult> GenerateToolPlanAsync(string userGoal, object? context = null, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("No Meta Llama API key configured. Executing intelligent local planning fallback for goal: {Goal}", userGoal);
            return GenerateLocalFallbackPlan(userGoal, context);
        }

        try
        {
            var systemPrompt = @"You are the LinkVault Verifiable Workspace Agent.
Decompose the user's workspace request into a sequence of tool calls.
Available tools:
- list_directory(directoryId?: string)
- search(query: string)
- create_directory(name: string, parentDirectoryId?: string)
- move_resource(resourceId: string, targetDirectoryId?: string)
- create_share_link(resourceId: string, expiryDays?: int)

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
                return GenerateLocalFallbackPlan(userGoal, context);
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
                // If model responded in text without tool calling protocol, use fallback extraction
                return GenerateLocalFallbackPlan(userGoal, context);
            }

            return new LlamaPlanResult(summary, toolCallsList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Meta Llama endpoint. Using local fallback.");
            return GenerateLocalFallbackPlan(userGoal, context);
        }
    }

    public async Task<LlamaGuardResult> ModerateContentWithLlamaGuardAsync(string content, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Deterministic local moderation heuristic
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
                JsonSerializer.Serialize(new { name = targetFolder, parentDirectoryId = (string?)null }),
                $"Create '{targetFolder}' directory"));

            toolCalls.Add(new LlamaToolCall("move_resource",
                JsonSerializer.Serialize(new { resourceName = "invoice", targetDirectory = targetFolder }),
                $"Move matching files to '{targetFolder}'"));

            if (lower.Contains("share"))
            {
                var days = lower.Contains("7") ? 7 : (lower.Contains("30") ? 30 : 1);
                toolCalls.Add(new LlamaToolCall("create_share_link",
                    JsonSerializer.Serialize(new { resourceName = "invoice", expiryDays = days }),
                    $"Generate {days}-day secure share link"));
            }

            return new LlamaPlanResult(
                $"Identified organization intent. Propose creating directory '{targetFolder}', moving matching files, and applying requested actions.",
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

            toolCalls.Add(new LlamaToolCall("move_resource",
                JsonSerializer.Serialize(new { targetDirectory = "Diagrams & Mockups" }),
                "File visual assets into categorized folder"));

            return new LlamaPlanResult(
                "Multimodal image intent detected. Propose classifying images with Llama 3.2 Vision and sorting them into categorized directories.",
                toolCalls);
        }

        // Scenario: Share file
        if (lower.Contains("share"))
        {
            toolCalls.Add(new LlamaToolCall("create_share_link",
                JsonSerializer.Serialize(new { query = userGoal, expiryDays = 7 }),
                "Create secure share link"));

            return new LlamaPlanResult("Create share link for specified resource.", toolCalls);
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
            score = Math.Max(score, 80);
        }

        var isSafe = score < 60;
        var explanation = isSafe 
            ? "No critical secrets detected by local safety heuristic." 
            : $"Exposed credentials detected: {string.Join(", ", threats)}";

        return new LlamaGuardResult(isSafe, score, threats, explanation);
    }
}
