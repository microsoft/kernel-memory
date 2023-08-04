// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Client.Models;

namespace Microsoft.SemanticMemory.Core.WebService;

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public TagCollection Tags { get; set; } = new();
}
