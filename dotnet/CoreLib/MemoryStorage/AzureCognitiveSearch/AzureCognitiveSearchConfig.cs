// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;

public class AzureCognitiveSearchConfig
{
    // TODO: add auth types
    public string Endpoint { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
    public string VectorIndexPrefix { get; set; } = string.Empty;
}
