// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Client.Models;

namespace Microsoft.SemanticMemory.Core.WebService;

public class MemoryQuery
{
    public string UserId { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public MemoryFilter Filter { get; set; } = new();
}
