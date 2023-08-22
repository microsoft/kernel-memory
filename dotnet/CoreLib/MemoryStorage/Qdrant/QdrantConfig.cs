// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;

namespace Microsoft.SemanticMemory.MemoryStorage.Qdrant;

public class QdrantConfig
{
    internal static readonly JsonSerializerOptions JSONOptions = new()
    {
        AllowTrailingCommas = true,
        MaxDepth = 10,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        WriteIndented = false
    };

    public string Endpoint { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
}
