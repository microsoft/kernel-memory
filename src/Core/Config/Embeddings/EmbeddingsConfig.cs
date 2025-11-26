using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.Embeddings;

/// <summary>
/// Base class for embeddings provider configurations
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(OllamaEmbeddingsConfig), typeDiscriminator: "ollama")]
[JsonDerivedType(typeof(OpenAIEmbeddingsConfig), typeDiscriminator: "openai")]
[JsonDerivedType(typeof(AzureOpenAIEmbeddingsConfig), typeDiscriminator: "azureOpenAI")]
public abstract class EmbeddingsConfig : IValidatable
{
    /// <summary>
    /// Type of embeddings provider
    /// </summary>
    [JsonIgnore]
    public abstract EmbeddingsTypes Type { get; }

    /// <summary>
    /// Validates the embeddings configuration
    /// </summary>
    /// <param name="path"></param>
    public abstract void Validate(string path);
}
