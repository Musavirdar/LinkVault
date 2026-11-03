namespace LinkVaultApi.Agent.Models;

public class MetaModelConfig
{
    public const string SectionName = "MetaModel";

    /// <summary>
    /// API Key is NEVER stored in configuration files; provided at runtime via META_MODEL_API_KEY.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Baseten OpenAI-compatible endpoint (chat completions at /chat/completions).
    /// </summary>
    public string BaseUrl { get; set; } = "https://inference.baseten.co/v1";

    /// <summary>
    /// Meta Muse Spark Open on Baseten. The hackathon guide mentions a "-1026" variant, but it was
    /// not available on the author's account (checked Nov 3), so the generally available name is used.
    /// Override with META_MODEL_NAME if your account exposes a different one.
    /// </summary>
    public string ModelName { get; set; } = "meta-models/muse-spark-open";

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
