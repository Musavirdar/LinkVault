namespace LinkVaultApi.Agent.Models;

public class MetaModelConfig
{
    public const string SectionName = "MetaModel";

    /// <summary>
    /// API Key is NEVER stored in configuration files; provided at runtime via META_MODEL_API_KEY.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Verified Baseten OpenAI-compatible endpoint.
    /// </summary>
    public string BaseUrl { get; set; } = "https://inference.baseten.co/v1";

    /// <summary>
    /// Verified Meta Hackathon model for Baseten: meta-models/muse-spark-open-1026.
    /// </summary>
    public string ModelName { get; set; } = "meta-models/muse-spark-open-1026";

    /// <summary>
    /// Optional guardrail model. If not set, META_GUARD_MODEL_NAME or ModelName is used.
    /// </summary>
    public string? GuardModelName { get; set; }

    /// <summary>
    /// Optional vision model. If not set, META_VISION_MODEL_NAME is checked.
    /// </summary>
    public string? VisionModelName { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}
