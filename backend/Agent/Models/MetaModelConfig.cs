namespace LinkVaultApi.Agent.Models;

public class MetaModelConfig
{
    public const string SectionName = "MetaModel";
    public const string LegacySectionName = "MetaLlama";

    /// <summary>
    /// API Key is NEVER stored in configuration files; provided at runtime via META_MODEL_API_KEY.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Verified Baseten OpenAI-compatible endpoint.
    /// </summary>
    public string BaseUrl { get; set; } = "https://inference.baseten.co/v1";

    /// <summary>
    /// Verified Meta Hackathon model supporting tools, JSON mode, reasoning, and image input.
    /// </summary>
    public string ModelName { get; set; } = "meta-models/muse-spark-open";

    public string MuseSparkModel
    {
        get => ModelName;
        set => ModelName = value;
    }

    public string MuseGlimmerModel { get; set; } = "meta-models/Muse-Glimmer-30B";

    public string VisionModel
    {
        get => ModelName; // Uses the same meta-models/muse-spark-open model with OpenAI image_url format
        set => ModelName = value;
    }

    public string LlamaGuardModel { get; set; } = "meta-llama/Llama-Guard-3-8B";

    // Compatibility aliases
    public string Llama33Model
    {
        get => ModelName;
        set => ModelName = value;
    }

    public string Llama31Model
    {
        get => MuseGlimmerModel;
        set => MuseGlimmerModel = value;
    }

    public string LlamaVisionModel
    {
        get => VisionModel;
        set => VisionModel = value;
    }

    public bool FallbackToLocal { get; set; } = true;
}
