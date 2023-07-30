// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.Core.Configuration;

public class SearchConfig
{
    public object? VectorDb { get; set; }

    public object? EmbeddingGenerator { get; set; }

    public object? TextGenerator { get; set; }
}
