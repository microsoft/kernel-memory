// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.KernelMemory;

// JSON serializable alternative to NameValueCollection
public class TagCollection : IDictionary<string, List<string?>>
{
    private readonly IDictionary<string, List<string?>> _data = new Dictionary<string, List<string?>>(StringComparer.OrdinalIgnoreCase);

    public ICollection<string> Keys { get { return this._data.Keys; } }

    public ICollection<List<string?>> Values { get { return this._data.Values; } }

    public IEnumerable<KeyValuePair<string, string?>> Pairs
    {
        get
        {
            return from key in this._data.Keys
                   from value in this._data[key]
                   select new KeyValuePair<string, string?>(key, value);
        }
    }

    public int Count { get { return this._data.Count; } }

    public bool IsReadOnly { get { return this._data.IsReadOnly; } }

    public List<string?> this[string key]
    {
        get => this._data[key];
        set
        {
            ValidateKey(key);
            this._data[key] = value;
        }
    }

    public IEnumerator<KeyValuePair<string, List<string?>>> GetEnumerator()
    {
        return this._data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public void Add(KeyValuePair<string, List<string?>> item)
    {
        ValidateKey(item.Key);
        this._data.Add(item);
    }

    public void Add(string key)
    {
        if (!this._data.ContainsKey(key))
        {
            this._data[key] = new List<string?>();
        }
    }

    public void Add(string key, string? value)
    {
        ValidateKey(key);
        // If the key exists
        if (this._data.TryGetValue(key, out List<string?>? list) && list != null)
        {
            if (value != null) { list.Add(value); }
        }
        else
        {
            // Add the key, but the value only if not null
            this._data[key] = value == null ? new List<string?>() : new List<string?> { value };
        }
    }

    public void Add(string key, List<string?> value)
    {
        ValidateKey(key);
        this._data.Add(key, value);
    }

    public bool TryGetValue(string key, out List<string?> value)
    {
        bool result = this._data.TryGetValue(key, out var valueOut);
        value = valueOut ?? new List<string?>();
        return result;
    }

    public bool Contains(KeyValuePair<string, List<string?>> item)
    {
        return this._data.Contains(item);
    }

    public bool ContainsKey(string key)
    {
        return this._data.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<string, List<string?>>[] array, int arrayIndex)
    {
        this._data.CopyTo(array, arrayIndex);
    }

    public void CopyTo(TagCollection tagCollection)
    {
        foreach (string key in this._data.Keys)
        {
            if (this._data[key] == null || this._data[key].Count == 0)
            {
                tagCollection.Add(key);
            }
            else
            {
                foreach (string? value in this._data[key])
                {
                    tagCollection.Add(key, value);
                }
            }
        }
    }

    public IEnumerable<KeyValuePair<string, string?>> ToKeyValueList()
    {
        return (from tag in this._data from tagValue in tag.Value select new KeyValuePair<string, string?>(tag.Key, tagValue));
    }

    public bool Remove(KeyValuePair<string, List<string?>> item)
    {
        return this._data.Remove(item);
    }

    public bool Remove(string key)
    {
        return this._data.Remove(key);
    }

    public void Clear()
    {
        this._data.Clear();
    }

    private static void ValidateKey(string key)
    {
        if (key.Contains(Constants.ReservedEqualsChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new KernelMemoryException($"A tag name cannot contain the '{Constants.ReservedEqualsChar}' char");
        }

        // '=' is reserved for backward/forward compatibility and to reduce URLs query params encoding complexity
        if (key.Contains('=', StringComparison.OrdinalIgnoreCase))
        {
            throw new KernelMemoryException("A tag name cannot contain the '=' char");
        }

        // ':' is reserved for backward/forward compatibility
        if (key.Contains(':', StringComparison.OrdinalIgnoreCase))
        {
            throw new KernelMemoryException("A tag name cannot contain the ':' char");
        }
    }
}
