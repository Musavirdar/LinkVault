namespace LinkVaultApi.Agent.Models;

public class MetaLlamaConfig
{
    public const string SectionName = "MetaLlama";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://bridge.baseten.co/v1";

    // Meta Hackathon: Official Muse model series (Meta Superintelligence Labs)
    public string MuseSparkModel { get; set; } = "meta-models/muse-spark-open";
    public string MuseGlimmerModel { get; set; } = "meta-models/Muse-Glimmer-30B";

    // Backwards-compatible aliases for Llama model naming
    public string Llama33Model
    {
        get => MuseSparkModel;
        set => MuseSparkModel = value;
    }

    public string Llama31Model
    {
        get => MuseGlimmerModel;
        set => MuseGlimmerModel = value;
    }

    public string LlamaVisionModel { get; set; } = "meta-llama/Llama-3.2-11B-Vision-Instruct";
    public string LlamaGuardModel { get; set; } = "meta-llama/Llama-Guard-3-8B";
    public bool FallbackToLocal { get; set; } = true;
}
