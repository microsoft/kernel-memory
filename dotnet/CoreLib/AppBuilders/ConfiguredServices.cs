// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SemanticMemory.Core.AppBuilders;

public class ConfiguredServices<T> where T : class
{
    private readonly List<Func<IServiceProvider, T>> _builders = new();

    public void Add(Func<IServiceProvider, T> builder)
    {
        this._builders.Add(builder);
    }

    public List<Func<IServiceProvider, T>> GetList()
    {
        return this._builders.Select(x => x).ToList();
    }
}
