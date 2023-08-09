// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.SemanticMemory.Client.Models;

public class MemoryFilter
{
    private readonly TagCollection _tags;

    public MemoryFilter()
    {
        this._tags = new TagCollection();
    }

    public bool IsEmpty()
    {
        return this._tags.Count == 0;
    }

    public MemoryFilter ByTag(string name, string value)
    {
        this._tags.Add(name, value);
        return this;
    }

    public MemoryFilter ByUser(string userId)
    {
        this._tags.Add(Constants.ReservedUserIdTag, userId);
        return this;
    }

    public MemoryFilter ByDocument(string docId)
    {
        this._tags.Add(Constants.ReservedPipelineIdTag, docId);
        return this;
    }

    public IEnumerable<KeyValuePair<string, string?>> GetFilters()
    {
        return this._tags.ToKeyValueList();
    }
}
