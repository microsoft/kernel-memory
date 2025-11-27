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
    /// Operation is queued and processed asynchronously.
    /// </summary>
    /// <param name="request">The upsert request containing content and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the content record (newly generated or existing).</returns>
    /// <exception cref="ContentStorageException">Thrown if queueing the operation fails.</exception>
    Task<string> UpsertAsync(UpsertRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes content by ID. Idempotent - no error if record doesn't exist.
    /// Operation is queued and processed asynchronously.
    /// </summary>
    /// <param name="id">The content ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    /// <exception cref="ContentStorageException">Thrown if queueing the operation fails.</exception>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

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
}
