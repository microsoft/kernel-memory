// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.SemanticMemory.AppBuilders;

/// <summary>
/// Represents a collection of service collections, so that DI helpers
/// like `WithX` act on multiple service collections, e.g. the one used
/// by MemoryClientBuilder and the one used by end user application.
/// </summary>
public class ServiceCollectionPool : IServiceCollection
{
    private readonly List<IServiceCollection> _pool;
    private bool _locked;

    public int Count => this._pool.First().Count;
    public bool IsReadOnly => this._pool.First().IsReadOnly;

    public ServiceCollectionPool(IServiceCollection sc)
    {
        this._locked = false;
        this._pool = new List<IServiceCollection> { sc };
    }

    public void AddServiceCollection(IServiceCollection sc)
    {
        if (this._locked)
        {
            throw new InvalidOperationException("The pool of service collections is already in use and cannot be extended");
        }

        this._pool.Add(sc);
    }

    public void Add(ServiceDescriptor item)
    {
        this._locked = true;
        foreach (var sc in this._pool)
        {
            sc.Add(item);
        }
    }

    public bool Contains(ServiceDescriptor item)
    {
        this._locked = true;
        return this._pool.First().Contains(item);
    }

    /**
     * The methods below are not used by MemoryClientBuilder and could lead to
     * unexpected bugs/behavior considering that the memory builder service
     * collection is different from the end user application service collection.
     */

    #region unnecessary - risky for collections that could be different

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public IEnumerator<ServiceDescriptor> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public int IndexOf(ServiceDescriptor item)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, ServiceDescriptor item)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    public ServiceDescriptor this[int index]
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Remove(ServiceDescriptor item)
    {
        throw new NotImplementedException();
    }

    #endregion
}
