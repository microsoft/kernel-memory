// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SemanticMemory.Core.Configuration;

public class TypeCollection<T> where T : class
{
    private readonly List<Type> _types;

    public void Add<TImplementation>() where TImplementation : T
    {
        this._types.Add(typeof(TImplementation));
    }

    public List<Type> GetList()
    {
        return this._types.Select(x => x).ToList();
    }

    public TypeCollection()
    {
        this._types = new();
    }

    public TypeCollection(Type firstValue)
    {
        this._types = new() { firstValue };
    }
}
