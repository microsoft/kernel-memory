// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.KernelMemory;

public class MemoryFilter : TagCollection
{
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

/// <summary>
/// Factory for <see cref="MemoryFilter"/>, to allow for a simpler syntax
/// Instead of: new MemoryFilter().ByDocument(id).ByTag(k, v)
/// Recommended: MemoryFilters.ByDocument(id).ByTag(k, v)
/// </summary>
public static class MemoryFilters
{
    public static MemoryFilter ByTag(string name, string value)
    {
        return new MemoryFilter().ByTag(name, value);
    }

    public static MemoryFilter ByDocument(string docId)
    {
        return new MemoryFilter().ByDocument(docId);
    }
}
