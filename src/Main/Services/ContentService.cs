// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Storage;
using KernelMemory.Core.Storage.Models;

namespace KernelMemory.Main.Services;

/// <summary>
/// Business logic layer for content operations.
/// Wraps IContentStorage and provides CLI-friendly interface.
/// </summary>
public class ContentService
{
    private readonly IContentStorage _storage;
    private readonly string _nodeId;

    /// <summary>
    /// Initializes a new instance of ContentService.
    /// </summary>
    /// <param name="storage">The content storage implementation.</param>
    /// <param name="nodeId">The node ID this service operates on.</param>
    public ContentService(IContentStorage storage, string nodeId)
    {
        this._storage = storage;
        this._nodeId = nodeId;
    }

    /// <summary>
    /// Gets the node ID this service operates on.
    /// </summary>
    public string NodeId => this._nodeId;

    /// <summary>
    /// Upserts content and returns the write result.
    /// </summary>
    /// <param name="request">The upsert request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>WriteResult with ID and completion status.</returns>
    public async Task<WriteResult> UpsertAsync(UpsertRequest request, CancellationToken cancellationToken = default)
    {
        return await this._storage.UpsertAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets content by ID.
    /// </summary>
    /// <param name="id">The content ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The content DTO, or null if not found.</returns>
    public async Task<ContentDto?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return await this._storage.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes content by ID.
    /// </summary>
    /// <param name="id">The content ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>WriteResult with ID and completion status.</returns>
    public async Task<WriteResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await this._storage.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists content with pagination.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of content DTOs.</returns>
    public async Task<List<ContentDto>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._storage.ListAsync(skip, take, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets total count of content items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total count.</returns>
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._storage.CountAsync(cancellationToken).ConfigureAwait(false);
    }
}
