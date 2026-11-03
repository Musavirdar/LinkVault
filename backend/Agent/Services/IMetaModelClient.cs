using System.Text.Json.Nodes;

namespace LinkVaultApi.Agent.Services;

public record ModelToolCall(string Id, string ToolName, string ArgumentsJson);

public record ModelChatMessage(
    string Role,
    string? Content,
    string? ToolCallId = null,
    List<ModelToolCall>? ToolCalls = null);

public record ModelChatResponse(
    string? Content,
    List<ModelToolCall> ToolCalls,
    string ModelName);

public record GuardScanResult(
    bool IsSafe,
    int RiskScore,
    List<string> FlaggedCategories,
    string Explanation);

public record VisionClassificationResult(
    string Category,
    string Description,
    double Confidence);

public interface IMetaModelClient
{
    string ModelName { get; }
    string BaseUrl { get; }
    bool HasApiKey { get; }
    bool HasVisionConfigured { get; }

    Task<ModelChatResponse> ChatCompletionAsync(
        List<ModelChatMessage> messages,
        JsonArray? tools = null,
        string? toolChoice = "auto",
        CancellationToken cancellationToken = default);

    Task<GuardScanResult> ScanContentSecurityAsync(
        string textSnippet,
        CancellationToken cancellationToken = default);

    Task<VisionClassificationResult?> ClassifyImageAsync(
        byte[] imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default);
}

