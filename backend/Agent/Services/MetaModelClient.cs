using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LinkVaultApi.Agent.Exceptions;
using LinkVaultApi.Agent.Models;
using Microsoft.Extensions.Options;

namespace LinkVaultApi.Agent.Services;

public class MetaModelClient : IMetaModelClient
{
    private readonly MetaModelConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MetaModelClient> _logger;

    public MetaModelClient(
        IOptions<MetaModelConfig> config,
        HttpClient httpClient,
        ILogger<MetaModelClient> logger)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _logger = logger;

        if (_config.TimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        }
    }

    private string ResolveApiKey()
    {
        var envKey = Environment.GetEnvironmentVariable("META_MODEL_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
            return envKey;

        return _config.ApiKey ?? string.Empty;
    }

    private string ResolveBaseUrl()
    {
        var envUrl = Environment.GetEnvironmentVariable("META_MODEL_BASE_URL");
        if (!string.IsNullOrWhiteSpace(envUrl))
            return envUrl;

        return string.IsNullOrWhiteSpace(_config.BaseUrl) ? "https://inference.baseten.co/v1" : _config.BaseUrl;
    }

    private string ResolveModelName()
    {
        var envModel = Environment.GetEnvironmentVariable("META_MODEL_NAME");
        if (!string.IsNullOrWhiteSpace(envModel))
            return envModel;

        return string.IsNullOrWhiteSpace(_config.ModelName) ? "meta-models/muse-spark-open-1026" : _config.ModelName;
    }

    private string ResolveGuardModelName()
    {
        var envModel = Environment.GetEnvironmentVariable("META_GUARD_MODEL_NAME");
        if (!string.IsNullOrWhiteSpace(envModel))
            return envModel;

        return !string.IsNullOrWhiteSpace(_config.GuardModelName) ? _config.GuardModelName : ResolveModelName();
    }

    private string? ResolveVisionModelName()
    {
        var envModel = Environment.GetEnvironmentVariable("META_VISION_MODEL_NAME");
        if (!string.IsNullOrWhiteSpace(envModel))
            return envModel;

        return !string.IsNullOrWhiteSpace(_config.VisionModelName) ? _config.VisionModelName : null;
    }

    public string ModelName => ResolveModelName();
    public string BaseUrl => ResolveBaseUrl();
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ResolveApiKey());
    public bool HasVisionConfigured => !string.IsNullOrWhiteSpace(ResolveVisionModelName());

    public static JsonArray GetWorkspaceToolsDefinition(bool includeVision = false)
    {
        var tools = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "list_directory",
                    ["description"] = "List contents of a directory in the workspace (read-only query).",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["directoryId"] = new JsonObject { ["type"] = "string", ["description"] = "Optional GUID of directory to inspect (null or omitted for root)" }
                        }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "search",
                    ["description"] = "Search workspace files and links by query keyword or name (read-only query).",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["query"] = new JsonObject { ["type"] = "string", ["description"] = "Search keyword or filename query" }
                        },
                        ["required"] = new JsonArray { "query" }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "create_directory",
                    ["description"] = "Create a new directory in the workspace (state-changing step).",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["name"] = new JsonObject { ["type"] = "string", ["description"] = "Name of the directory to create" },
                            ["parentDirectoryId"] = new JsonObject { ["type"] = "string", ["description"] = "Optional GUID of parent directory" }
                        },
                        ["required"] = new JsonArray { "name" }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "move_resource",
                    ["description"] = "Move an existing file or resource into a target directory (state-changing step).",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["resourceId"] = new JsonObject { ["type"] = "string", ["description"] = "GUID of the resource to move" },
                            ["targetDirectoryId"] = new JsonObject { ["type"] = "string", ["description"] = "Destination directory GUID (or null for root)" }
                        },
                        ["required"] = new JsonArray { "resourceId" }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "create_share_link",
                    ["description"] = "Generate a secure, time-bound public share link for a resource (state-changing step).",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["resourceId"] = new JsonObject { ["type"] = "string", ["description"] = "GUID of the resource to share" },
                            ["expiryDays"] = new JsonObject { ["type"] = "integer", ["description"] = "Lifetime of share link in days (1-365)" },
                            ["password"] = new JsonObject { ["type"] = "string", ["description"] = "Optional access password" },
                            ["maxDownloads"] = new JsonObject { ["type"] = "integer", ["description"] = "Optional maximum download limit" }
                        },
                        ["required"] = new JsonArray { "resourceId" }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "delete_resource",
                    ["description"] = "Move a resource to trash / mark as deleted (state-changing step).",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["resourceId"] = new JsonObject { ["type"] = "string", ["description"] = "GUID of the resource to delete" }
                        },
                        ["required"] = new JsonArray { "resourceId" }
                    }
                }
            }
        };

        if (includeVision)
        {
            tools.Add(new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "classify_image",
                    ["description"] = "Categorize an uploaded image asset using Meta multimodal vision.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["resourceId"] = new JsonObject { ["type"] = "string", ["description"] = "GUID of the image resource to classify" }
                        },
                        ["required"] = new JsonArray { "resourceId" }
                    }
                }
            });
        }

        return tools;
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
                case "list_directory":
                    if (root.TryGetProperty("directoryId", out var lProp) && lProp.ValueKind != JsonValueKind.Null)
                    {
                        if (lProp.ValueKind != JsonValueKind.String || !Guid.TryParse(lProp.GetString(), out _))
                            return (false, "Tool 'list_directory' parameter 'directoryId' must be a valid GUID.");
                    }
                    break;

                case "search":
                    if (!root.TryGetProperty("query", out var qProp) || qProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(qProp.GetString()))
                        return (false, "Tool 'search' requires non-empty string parameter 'query'.");
                    break;

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

                case "delete_resource":
                    if (!root.TryGetProperty("resourceId", out var delProp) || delProp.ValueKind != JsonValueKind.String || !Guid.TryParse(delProp.GetString(), out var delGuid) || delGuid == Guid.Empty)
                        return (false, "Tool 'delete_resource' requires a valid non-empty GUID parameter 'resourceId'.");
                    break;

                case "classify_image":
                    if (!root.TryGetProperty("resourceId", out var imgProp) || imgProp.ValueKind != JsonValueKind.String || !Guid.TryParse(imgProp.GetString(), out var imgGuid) || imgGuid == Guid.Empty)
                        return (false, "Tool 'classify_image' requires a valid non-empty GUID parameter 'resourceId'.");
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

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken)
    {
        using var firstRequest = createRequest();
        var response = await _httpClient.SendAsync(firstRequest, cancellationToken);

        // One retry on 429 or 5xx
        if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
        {
            _logger.LogWarning("Meta model API returned status {StatusCode}. Retrying once after 1 second...", response.StatusCode);
            await Task.Delay(1000, cancellationToken);
            using var retryRequest = createRequest();
            response = await _httpClient.SendAsync(retryRequest, cancellationToken);
        }

        return response;
    }

    public async Task<ModelChatResponse> ChatCompletionAsync(
        List<ModelChatMessage> messages,
        JsonArray? tools = null,
        string? toolChoice = "auto",
        CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        var baseUrl = ResolveBaseUrl();
        var modelName = ResolveModelName();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ModelUnavailableException("Meta Model API key is not configured. Set the META_MODEL_API_KEY environment variable.", modelName);
        }

        var messagesArray = new JsonArray();
        foreach (var msg in messages)
        {
            var msgObj = new JsonObject { ["role"] = msg.Role };

            if (msg.Content != null)
            {
                msgObj["content"] = msg.Content;
            }
            else
            {
                msgObj["content"] = null;
            }

            if (!string.IsNullOrWhiteSpace(msg.ToolCallId))
            {
                msgObj["tool_call_id"] = msg.ToolCallId;
            }

            if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                var callsArray = new JsonArray();
                foreach (var tc in msg.ToolCalls)
                {
                    callsArray.Add(new JsonObject
                    {
                        ["id"] = tc.Id,
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = tc.ToolName,
                            ["arguments"] = tc.ArgumentsJson
                        }
                    });
                }
                msgObj["tool_calls"] = callsArray;
            }

            messagesArray.Add(msgObj);
        }

        var requestBody = new JsonObject
        {
            ["model"] = modelName,
            ["messages"] = messagesArray,
            ["temperature"] = 0.1
        };

        if (tools != null && tools.Count > 0)
        {
            requestBody["tools"] = tools;
            if (!string.IsNullOrWhiteSpace(toolChoice))
            {
                requestBody["tool_choice"] = toolChoice;
            }
        }

        var endpoint = $"{baseUrl.TrimEnd('/')}/chat/completions";
        var payloadJson = requestBody.ToJsonString();

        HttpRequestMessage CreateRequest()
        {
            var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            return req;
        }

        HttpResponseMessage response;
        try
        {
            response = await SendWithRetryAsync(CreateRequest, cancellationToken);
        }
        catch (Exception ex) when (ex is not ModelUnavailableException)
        {
            _logger.LogError(ex, "Failed to connect to Meta model endpoint {Endpoint}", endpoint);
            throw new ModelUnavailableException($"Unable to connect to Meta model endpoint at {endpoint}: {ex.Message}", modelName, ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Meta model API failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errBody);
            throw new ModelUnavailableException($"Meta model API returned status {(int)response.StatusCode} ({response.StatusCode}): {errBody}", modelName);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonNode.Parse(responseJson);
        var choice = parsed?["choices"]?[0]?["message"];

        var toolCallsList = new List<ModelToolCall>();
        if (choice?["tool_calls"] is JsonArray calls)
        {
            foreach (var call in calls)
            {
                var callId = call?["id"]?.ToString() ?? Guid.NewGuid().ToString();
                var fnName = call?["function"]?["name"]?.ToString() ?? "unknown";

                string fnArgs = "{}";
                var argsNode = call?["function"]?["arguments"];
                if (argsNode is JsonValue val && val.TryGetValue<string>(out var strVal))
                {
                    fnArgs = strVal;
                }
                else if (argsNode != null)
                {
                    fnArgs = argsNode.ToString();
                }

                toolCallsList.Add(new ModelToolCall(callId, fnName, fnArgs));
            }
        }

        // Strictly ignore reasoning_content; only read message.content
        var content = choice?["content"]?.ToString();
        return new ModelChatResponse(content, toolCallsList, modelName);
    }

    public async Task<GuardScanResult> ScanContentSecurityAsync(string textSnippet, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        var baseUrl = ResolveBaseUrl();
        var guardModel = ResolveGuardModelName();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new GuardScanResult(true, 0, new List<string>(), "Model scan skipped: META_MODEL_API_KEY is not configured.");
        }

        try
        {
            var prompt = $@"You are a security moderation analyzer. Inspect the following text snippet (max 8KB) for leaked credentials, passwords, cryptographic private keys, AWS/cloud tokens, and connection strings.
Format your response STRICTLY as valid JSON with keys:
- 'isSafe' (boolean): true if no sensitive secrets detected, false otherwise.
- 'riskScore' (integer 0-100): estimated risk severity.
- 'flaggedCategories' (array of strings): names of detected secret types.
- 'explanation' (string): brief explanation.

Text snippet:
{textSnippet[..Math.Min(textSnippet.Length, 8192)]}";

            var requestBody = new JsonObject
            {
                ["model"] = guardModel,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "user", ["content"] = prompt }
                },
                ["temperature"] = 0.0
            };

            var endpoint = $"{baseUrl.TrimEnd('/')}/chat/completions";
            var payload = requestBody.ToJsonString();

            HttpRequestMessage CreateRequest()
            {
                var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                return req;
            }

            var response = await SendWithRetryAsync(CreateRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Guard model returned non-success status {StatusCode}: {Error}", response.StatusCode, err);
                return new GuardScanResult(true, 0, new List<string>(), $"Guard model unavailable ({response.StatusCode}). Deterministic regex active.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonNode.Parse(json);
            var content = parsed?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "{}";

            // Parse response JSON
            using var doc = JsonDocument.Parse(content);
            var isSafe = doc.RootElement.GetProperty("isSafe").GetBoolean();
            var riskScore = doc.RootElement.GetProperty("riskScore").GetInt32();
            var categories = new List<string>();
            if (doc.RootElement.TryGetProperty("flaggedCategories", out var catArr))
            {
                foreach (var c in catArr.EnumerateArray())
                {
                    categories.Add(c.GetString() ?? "");
                }
            }
            var explanation = doc.RootElement.TryGetProperty("explanation", out var exp) ? exp.GetString() ?? "" : "";

            return new GuardScanResult(isSafe, riskScore, categories, explanation);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Guard model scan. Falling back to deterministic scan result.");
            return new GuardScanResult(true, 0, new List<string>(), $"Guard model scan error: {ex.Message}");
        }
    }

    public async Task<VisionClassificationResult?> ClassifyImageAsync(byte[] imageBytes, string mimeType, CancellationToken cancellationToken = default)
    {
        var visionModel = ResolveVisionModelName();
        if (string.IsNullOrWhiteSpace(visionModel))
        {
            // Vision model is not configured; per rules, do not mock output
            return null;
        }

        var apiKey = ResolveApiKey();
        var baseUrl = ResolveBaseUrl();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        // Cap image size at 4 MB
        if (imageBytes.Length > 4 * 1024 * 1024)
        {
            throw new ArgumentException("Image size exceeds maximum 4 MB limit for vision analysis.");
        }

        string[] allowedMimeTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
        var normalizedMime = mimeType?.ToLowerInvariant() ?? "";
        if (!allowedMimeTypes.Contains(normalizedMime))
        {
            throw new ArgumentException($"MIME type '{mimeType}' is not supported for vision analysis. Allowed types: {string.Join(", ", allowedMimeTypes)}.");
        }

        try
        {
            var base64Data = Convert.ToBase64String(imageBytes);
            var prompt = "Categorize this workspace image into one of: 'Architecture Diagrams', 'UI Mockups', 'Receipts & Invoices', 'Photos & Media', 'Documents'. Respond in JSON with keys 'category', 'description', 'confidence'.";

            var requestBody = new JsonObject
            {
                ["model"] = visionModel,
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

            var endpoint = $"{baseUrl.TrimEnd('/')}/chat/completions";
            var payload = requestBody.ToJsonString();

            HttpRequestMessage CreateRequest()
            {
                var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                return req;
            }

            var response = await SendWithRetryAsync(CreateRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonNode.Parse(json);
            var content = parsed?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "{}";

            using var doc = JsonDocument.Parse(content);
            var category = doc.RootElement.GetProperty("category").GetString() ?? "Documents";
            var desc = doc.RootElement.GetProperty("description").GetString() ?? "";
            var conf = doc.RootElement.TryGetProperty("confidence", out var cf) ? cf.GetDouble() : 0.85;

            return new VisionClassificationResult(category, desc, conf);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to classify image with Meta Vision model.");
            return null;
        }
    }
}

