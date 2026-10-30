using LinkVaultApi.Agent.Models;

namespace LinkVaultApi.Agent.Services;

public record LlamaToolCall(string ToolName, string ArgumentsJson, string? Description = null);
public record LlamaPlanResult(string Summary, List<LlamaToolCall> ToolCalls);
public record LlamaGuardResult(bool IsSafe, int RiskScore, List<string> FlaggedCategories, string Explanation);
public record LlamaVisionResult(string Category, string Description, double Confidence);

public interface IMetaLlamaClient
{
    Task<LlamaPlanResult> GenerateToolPlanAsync(string userGoal, object? context = null, CancellationToken cancellationToken = default);
    Task<LlamaGuardResult> ModerateContentWithLlamaGuardAsync(string content, CancellationToken cancellationToken = default);
    Task<LlamaVisionResult> ClassifyImageWithVisionAsync(byte[] imageBytes, string mimeType, CancellationToken cancellationToken = default);
}
