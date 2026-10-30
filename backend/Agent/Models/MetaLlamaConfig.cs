namespace LinkVaultApi.Agent.Models;

public class MetaLlamaConfig
{
    public const string SectionName = "MetaLlama";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://bridge.baseten.co/v1";
    public string Llama33Model { get; set; } = "meta-llama/Llama-3.3-70B-Instruct";
    public string Llama31Model { get; set; } = "meta-llama/Llama-3.1-8B-Instruct";
    public string LlamaVisionModel { get; set; } = "meta-llama/Llama-3.2-11B-Vision-Instruct";
    public string LlamaGuardModel { get; set; } = "meta-llama/Llama-Guard-3-8B";
    public bool FallbackToLocal { get; set; } = true;
}
