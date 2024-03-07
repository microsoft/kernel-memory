// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

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
