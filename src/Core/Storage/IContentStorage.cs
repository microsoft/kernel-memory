// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Storage.Exceptions;
using KernelMemory.Core.Storage.Models;

namespace KernelMemory.Core.Storage;

/// <summary>
/// Interface for content storage operations.
/// Provides queue-based write operations with eventual consistency.
/// </summary>
public interface IContentStorage
{
    /// <summary>
    /// Upserts content. Creates new record if ID is empty, replaces existing if ID is provided.
    /// Operation is queued and processed synchronously (best-effort).
    /// Never throws after queue succeeds - returns WriteResult with completion status.
    /// </summary>
    /// <param name="request">The upsert request containing content and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>WriteResult with ID and completion status.</returns>
    /// <exception cref="ContentStorageException">Thrown only if queueing the operation fails.</exception>
    Task<WriteResult> UpsertAsync(UpsertRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes content by ID. Idempotent - no error if record doesn't exist.
    /// Operation is queued and processed synchronously (best-effort).
    /// Never throws after queue succeeds - returns WriteResult with completion status.
    /// </summary>
    /// <param name="id">The content ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>WriteResult with ID and completion status.</returns>
    /// <exception cref="ContentStorageException">Thrown only if queueing the operation fails.</exception>
    Task<WriteResult> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves content by ID.
    /// </summary>
    /// <param name="id">The content ID to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The content DTO, or null if not found.</returns>
    Task<ContentDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts total number of content records.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total count of content records.</returns>
    Task<long> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists content records with pagination support.
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of content DTOs.</returns>
    Task<List<ContentDto>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);
}
