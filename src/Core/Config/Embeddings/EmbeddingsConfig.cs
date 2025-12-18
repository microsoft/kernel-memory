// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Config.Embeddings;

/// <summary>
/// Base class for embeddings provider configurations
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(OllamaEmbeddingsConfig), typeDiscriminator: "ollama")]
[JsonDerivedType(typeof(OpenAIEmbeddingsConfig), typeDiscriminator: "openai")]
[JsonDerivedType(typeof(AzureOpenAIEmbeddingsConfig), typeDiscriminator: "azureOpenAI")]
[JsonDerivedType(typeof(HuggingFaceEmbeddingsConfig), typeDiscriminator: "huggingFace")]
public abstract class EmbeddingsConfig : IValidatable
{
    /// <summary>
    /// Type of embeddings provider
    /// </summary>
    [JsonIgnore]
    public abstract EmbeddingsTypes Type { get; }

    /// <summary>
    /// Maximum number of texts to send per embeddings API request.
    /// Providers that support batch requests should chunk input using this size.
    /// Default: 10.
    /// </summary>
    [JsonPropertyName("batchSize")]
    public int BatchSize { get; set; } = Constants.EmbeddingDefaults.DefaultBatchSize;

    /// <summary>
    /// Validates the embeddings configuration
    /// </summary>
    /// <param name="path"></param>
    public abstract void Validate(string path);
}
