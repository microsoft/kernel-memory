// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.SemanticMemory.Core.Configuration.Dynamic;

namespace Microsoft.SemanticMemory.Core.Configuration;

/// <summary>
/// Configuration settings for the embedding generators
/// </summary>
public class EmbeddingGeneratorsConfig
{
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

        return this.EmbeddingGenerators[position].ToEmbeddingGenerationConfig();
    }
}
