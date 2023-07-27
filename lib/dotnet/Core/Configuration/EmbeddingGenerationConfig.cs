// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Core.Configuration;

/// <summary>
/// Configuration settings for the embedding generators
/// </summary>
public class EmbeddingGenerationConfig
{
    public class TypedItem
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public GeneratorTypes Type { get; set; }
    }

    /// <summary>
    /// Supported embedding generator types.
    /// TODO: add SentenceTransformers
    /// </summary>
    public enum GeneratorTypes
    {
        Unknown = 0,
        AzureOpenAI = 1,
        OpenAI = 2,
    }

    /// <summary>
    /// List of vector storage configurations to use. Normally just one
    /// but it's also possible to store embeddings on multiple services at the same time.
    /// </summary>
    public List<Dictionary<string, object>> EmbeddingGenerators { get; set; } = new();

    /// <summary>
    /// Deserialize and cast a configuration item to the proper configuration type
    /// </summary>
    /// <param name="position">Position in the <see cref="EmbeddingGenerators"/> property</param>
    /// <returns>Configuration object</returns>
    public object GetEmbeddingGeneratorConfig(int position)
    {
        if (this.EmbeddingGenerators.Count < position + 1)
        {
            throw new ConfigurationException($"List doesn't contain an element at position {position}");
        }

        var json = JsonSerializer.Serialize(this.EmbeddingGenerators[position]);
        var typedItem = JsonSerializer.Deserialize<TypedItem>(json);
        object? result;

        switch (typedItem?.Type)
        {
            case null:
                throw new ConfigurationException("Embedding generator type is NULL");

            default:
                throw new ConfigurationException($"Embedding generator type not supported: {typedItem.Type:G}");

            case GeneratorTypes.AzureOpenAI:
                result = JsonSerializer.Deserialize<AzureOpenAIConfig>(json);
                break;

            case GeneratorTypes.OpenAI:
                result = JsonSerializer.Deserialize<OpenAIConfig>(json);
                break;
        }

        if (result == null)
        {
            throw new ConfigurationException($"Unable to deserialize {typedItem.Type} configuration at position {position}");
        }

        return result;
    }
}
