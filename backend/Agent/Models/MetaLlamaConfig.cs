namespace LinkVaultApi.Agent.Models;

public class MetaLlamaConfig
{
    public const string SectionName = "MetaLlama";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public string Llama33Model { get; set; } = "llama-3.3-70b-versatile";
    public string Llama31Model { get; set; } = "llama-3.1-8b-instant";
    public string LlamaVisionModel { get; set; } = "llama-3.2-11b-vision-preview";
    public string LlamaGuardModel { get; set; } = "llama-guard-3-8b";
    public bool FallbackToLocal { get; set; } = true;
}
