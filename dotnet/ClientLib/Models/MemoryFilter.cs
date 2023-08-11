// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.SemanticMemory.Client.Models;

public class MemoryFilter : TagCollection
{
    public float MinRelevance { get; set; } = 0.5f;

    public bool IsEmpty()
    {
        return this.Count == 0;
    }

    public MemoryFilter ByTag(string name, string value)
    {
        this.Add(name, value);
        return this;
    }

    public MemoryFilter ByUser(string userId)
    {
        this.Add(Constants.ReservedUserIdTag, userId);
        return this;
    }

    public MemoryFilter ByDocument(string docId)
    {
        this.Add(Constants.ReservedPipelineIdTag, docId);
        return this;
    }

    public IEnumerable<KeyValuePair<string, string?>> GetFilters()
    {
        return this.ToKeyValueList();
    }
}
