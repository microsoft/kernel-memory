// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Storage.Models;

/// <summary>
/// Result of a write operation (upsert or delete).
/// Never throws after queue succeeds - always returns this result.
/// </summary>
public sealed class WriteResult
{
    /// <summary>
    /// The content ID (newly generated or existing).
    /// Always populated when queue succeeds.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// True if all steps completed successfully.
    /// False if operation is queued but not yet fully processed.
    /// </summary>
    public required bool Completed { get; init; }

    /// <summary>
    /// True if operation was queued but not fully completed.
    /// Inverse of Completed for convenience.
    /// </summary>
    public bool Queued => !this.Completed;

    /// <summary>
    /// Error message if any step failed.
    /// Empty string if no error occurred.
    /// </summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// Creates a successful result (all steps completed).
    /// </summary>
    /// <param name="id">The content ID.</param>
    public static WriteResult Success(string id) => new()
    {
        Id = id,
        Completed = true,
        Error = string.Empty
    };

    /// <summary>
    /// Creates a queued result (operation accepted but not fully completed).
    /// </summary>
    /// <param name="id">The content ID.</param>
    /// <param name="error">Optional error message describing what failed.</param>
    public static WriteResult QueuedOnly(string id, string error = "") => new()
    {
        Id = id,
        Completed = false,
        Error = error
    };
}
