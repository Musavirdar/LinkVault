using System.Text.Json.Nodes;
using LinkVaultApi.Agent.Exceptions;
using LinkVaultApi.Agent.Services;

namespace LinkVaultApi.Tests.Fakes;

public class FakeMetaModelClient : IMetaModelClient
{
    public string ModelName { get; set; } = "meta-models/muse-spark-open-1026";
    public string BaseUrl { get; set; } = "https://inference.baseten.co/v1";
    public bool HasApiKey { get; set; } = true;
    public bool HasVisionConfigured { get; set; } = true;

    public Func<List<ModelChatMessage>, JsonArray?, string?, ModelChatResponse>? OnChatCompletion { get; set; }
    public Func<string, GuardScanResult>? OnScanSecurity { get; set; }
    public Func<byte[], string, VisionClassificationResult?>? OnClassifyImage { get; set; }

    public int ChatCallCount { get; private set; }

    public Task<ModelChatResponse> ChatCompletionAsync(
        List<ModelChatMessage> messages,
        JsonArray? tools = null,
        string? toolChoice = "auto",
        CancellationToken cancellationToken = default)
    {
        ChatCallCount++;
        if (OnChatCompletion != null)
        {
            return Task.FromResult(OnChatCompletion(messages, tools, toolChoice));
        }

        return Task.FromResult(new ModelChatResponse("Default fake assistant response", new List<ModelToolCall>(), ModelName));
    }

    public Task<GuardScanResult> ScanContentSecurityAsync(string textSnippet, CancellationToken cancellationToken = default)
    {
        if (OnScanSecurity != null)
        {
            return Task.FromResult(OnScanSecurity(textSnippet));
        }

        return Task.FromResult(new GuardScanResult(true, 0, new List<string>(), "Clean"));
    }

    public Task<VisionClassificationResult?> ClassifyImageAsync(byte[] imageBytes, string mimeType, CancellationToken cancellationToken = default)
    {
        if (OnClassifyImage != null)
        {
            return Task.FromResult(OnClassifyImage(imageBytes, mimeType));
        }

        return Task.FromResult<VisionClassificationResult?>(new VisionClassificationResult("Architecture Diagrams", "Fake diagram", 0.95));
    }
}

