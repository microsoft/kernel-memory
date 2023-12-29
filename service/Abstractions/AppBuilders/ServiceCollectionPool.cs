// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.KernelMemory.AppBuilders;

/// <summary>
/// Represents a collection of service collections, so that DI helpers
/// like `WithX` act on multiple service collections, e.g. the one used
/// by KernelMemoryBuilder and the one used by end user application.
///
/// The pool is meant to have a "primary" that contains all services,
/// so that it's possible to look up the aggregate, e.g. check if
/// a dependency exists in any of the collections, and to loop
/// through the complete list of service descriptors.
/// </summary>
public class ServiceCollectionPool : IServiceCollection
{
    /// <summary>
    /// Collection of service collections, ie the pool.
    /// </summary>
    private readonly List<IServiceCollection> _pool;

    /// <summary>
    /// Primary collection used for read and iteration calls
    /// </summary>
    private readonly IServiceCollection _primaryCollection;

    /// <summary>
    /// Flag indicating whether the list of collections is readonly.
    /// The list becomes readonly as soon as service descriptors are added.
    /// </summary>
    private bool _poolSizeLocked;

    /// <summary>
    /// The total number of service descriptors registered
    /// </summary>
    public int Count => this._primaryCollection.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => this._primaryCollection.IsReadOnly;

    /// <summary>
    /// Create a new instance, passing in the primary list of services
    /// </summary>
    /// <param name="primaryCollection">The primary service collection</param>
    public ServiceCollectionPool(IServiceCollection primaryCollection)
    {
        if (primaryCollection == null)
        {
            throw new ArgumentNullException(nameof(primaryCollection), "The primary service collection cannot be NULL");
        }

        this._poolSizeLocked = false;
        this._primaryCollection = primaryCollection;
        this._pool = new List<IServiceCollection> { primaryCollection };
    }

    /// <summary>
    /// Add one more service collection to the pool
    /// </summary>
    /// <param name="serviceCollection">Service collection</param>
    public void AddServiceCollection(IServiceCollection? serviceCollection)
    {
        if (serviceCollection == null) { return; }

        if (this._poolSizeLocked)
        {
            throw new InvalidOperationException("The pool of service collections is already in use and cannot be extended");
        }

        this._pool.Add(serviceCollection);
    }

    /// <inheritdoc/>
    public void Add(ServiceDescriptor item)
    {
        this.Lock();
        foreach (var sc in this._pool)
        {
            sc.Add(item);
        }
    }

    /// <inheritdoc/>
    public bool Contains(ServiceDescriptor item)
    {
        this.Lock();
        return this._pool.First().Contains(item);
    }

    /* IMPORTANT: iterations use the primary collection only. */

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        this.Lock();
        return this._primaryCollection.GetEnumerator();
    }

    /// <inheritdoc/>
    public IEnumerator<ServiceDescriptor> GetEnumerator()
    {
        this.Lock();
        return this._primaryCollection.GetEnumerator();
    }

    #region unsafe

    /// <inheritdoc/>
    public bool Remove(ServiceDescriptor item)
    {
        this.Lock();
        DeletionsNotAllowed();
        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        this.Lock();
        DeletionsNotAllowed();
    }

    /// <inheritdoc/>
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        this.Lock();
        throw AccessByPositionNotAllowed();
    }

    /// <inheritdoc/>
    public int IndexOf(ServiceDescriptor item)
    {
        this.Lock();
        throw AccessByPositionNotAllowed();
    }

    /// <inheritdoc/>
    public void Insert(int index, ServiceDescriptor item)
    {
        this.Lock();
        throw AccessByPositionNotAllowed();
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        this.Lock();
        throw AccessByPositionNotAllowed();
    }

    /// <inheritdoc/>
    public ServiceDescriptor this[int index]
    {
        get
        {
            this.Lock();
            throw AccessByPositionNotAllowed();
        }
        set
        {
            this.Lock();
            throw AccessByPositionNotAllowed();
        }
    }

    #endregion

    private void Lock()
    {
        this._poolSizeLocked = true;
    }

    /// <exception cref="InvalidOperationException"></exception>
    private static InvalidOperationException DeletionsNotAllowed()
    {
        return new InvalidOperationException(
            $"{nameof(ServiceCollectionPool)} is used to share external service collections with KernelBuilder. " +
            $"KernelBuilder should never remove service descriptors defined in the hosting application.");
    }

    /// <exception cref="InvalidOperationException"></exception>
    private static InvalidOperationException AccessByPositionNotAllowed()
    {
        return new InvalidOperationException(
            $"{nameof(ServiceCollectionPool)} contains collections of different size, " +
            "and direct access by position is not allowed, to avoid inconsistent results.");
    }

#pragma warning restore CA1065
}
