namespace LinkVaultApi.Agent.Models;

/// <summary>
/// Backwards-compatible configuration subclass pointing to MetaModelConfig.
/// </summary>
public class MetaLlamaConfig : MetaModelConfig
{
    public new const string SectionName = "MetaLlama";
}
