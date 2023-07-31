// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.SemanticMemory.Core.Configuration.Dynamic;

namespace Microsoft.SemanticMemory.Core.Configuration;

public class SearchConfig
{
    public Dictionary<string, object> VectorDb { get; set; } = new();

    public Dictionary<string, object> EmbeddingGenerator { get; set; } = new();

    public Dictionary<string, object> TextGenerator { get; set; } = new();

    public object GetVectorDbConfig()
    {
        return this.VectorDb.ToVectorDbConfig();
    }

    public object GetEmbeddingGeneratorConfig()
    {
        return this.EmbeddingGenerator.ToEmbeddingGenerationConfig();
    }

    public object GetTextGeneratorConfig()
    {
        return this.TextGenerator.ToTextGenerationConfig();
    }
}
