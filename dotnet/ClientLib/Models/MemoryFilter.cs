// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.SemanticMemory;

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

    public MemoryFilter ByDocument(string docId)
    {
        this.Add(Constants.ReservedDocumentIdTag, docId);
        return this;
    }

    public IEnumerable<KeyValuePair<string, string?>> GetFilters()
    {
        return this.ToKeyValueList();
    }
}
