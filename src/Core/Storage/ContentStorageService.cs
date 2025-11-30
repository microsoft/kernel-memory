// Copyright (c) Microsoft. All rights reserved.
using System.Text;
using System.Text.Json;
using KernelMemory.Core.Search;
using KernelMemory.Core.Storage.Entities;
using KernelMemory.Core.Storage.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Core.Storage;

/// <summary>
/// Implementation of IContentStorage using SQLite with queue-based execution.
/// Follows two-phase write pattern with distributed locking.
/// Integrates with multiple search indexes (FTS, vector, graph, etc.).
/// </summary>
public class ContentStorageService : IContentStorage
{
    private readonly ContentStorageDbContext _context;
    private readonly ICuidGenerator _cuidGenerator;
    private readonly ILogger<ContentStorageService> _logger;
    private readonly IReadOnlyDictionary<string, ISearchIndex> _searchIndexById;

    /// <summary>
    /// Initializes ContentStorageService without search indexes.
    /// </summary>
    /// <param name="context">Database context for content storage.</param>
    /// <param name="cuidGenerator">Generator for unique content IDs.</param>
    /// <param name="logger">Logger instance.</param>
    public ContentStorageService(
        ContentStorageDbContext context,
        ICuidGenerator cuidGenerator,
        ILogger<ContentStorageService> logger)
        : this(context, cuidGenerator, logger, new Dictionary<string, ISearchIndex>())
    {
    }

    /// <summary>
    /// Initializes ContentStorageService with search indexes.
    /// </summary>
    /// <param name="context">Database context for content storage.</param>
    /// <param name="cuidGenerator">Generator for unique content IDs.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="searchIndexById">Dictionary of index ID to ISearchIndex instance.</param>
    public ContentStorageService(
        ContentStorageDbContext context,
        ICuidGenerator cuidGenerator,
        ILogger<ContentStorageService> logger,
        IReadOnlyDictionary<string, ISearchIndex> searchIndexById)
    {
        this._context = context;
        this._cuidGenerator = cuidGenerator;
        this._logger = logger;
        this._searchIndexById = searchIndexById;
    }

    /// <summary>
    /// Upserts content following the two-phase write pattern.
    /// Never throws after queue succeeds - returns WriteResult with completion status.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Best-effort error handling for phase 2 and processing - operation is queued successfully in phase 1")]
    public async Task<WriteResult> UpsertAsync(UpsertRequest request, CancellationToken cancellationToken = default)
    {
        // Generate ID if not provided
        var contentId = string.IsNullOrWhiteSpace(request.Id)
            ? this._cuidGenerator.Generate()
            : request.Id;

        this._logger.LogInformation("Starting upsert operation for content ID: {ContentId}", contentId);

        // Phase 1: Queue the operation (MUST succeed - throws if fails)
        var operationId = await this.QueueUpsertOperationAsync(contentId, request, cancellationToken).ConfigureAwait(false);
        this._logger.LogDebug("Phase 1 complete: Operation {OperationId} queued for content {ContentId}", operationId, contentId);

        // Phase 2: Try to cancel superseded operations (best effort)
        try
        {
            await this.TryCancelSupersededUpsertOperationsAsync(contentId, operationId, cancellationToken).ConfigureAwait(false);
            this._logger.LogDebug("Phase 2 complete: Cancelled superseded operations for content {ContentId}", contentId);
        }
        catch (Exception ex)
        {
            // Best effort - log but don't fail
            this._logger.LogWarning(ex, "Phase 2 failed to cancel superseded operations for content {ContentId} - continuing anyway", contentId);
        }

        // Processing: Try to process the new operation synchronously
        try
        {
            await this.TryProcessNextOperationAsync(contentId, cancellationToken).ConfigureAwait(false);
            this._logger.LogDebug("Processing complete for content {ContentId}", contentId);
            return WriteResult.Success(contentId);
        }
        catch (Exception ex)
        {
            // Log but don't fail - operation is queued and will be processed eventually
            this._logger.LogWarning(ex, "Failed to process operation synchronously for content {ContentId} - will be processed by background worker", contentId);
            return WriteResult.QueuedOnly(contentId, ex.Message);
        }
    }

    /// <summary>
    /// Deletes content following the two-phase write pattern.
    /// Never throws after queue succeeds - returns WriteResult with completion status.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Best-effort error handling for phase 2 and processing - operation is queued successfully in phase 1")]
    public async Task<WriteResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation("Starting delete operation for content ID: {ContentId}", id);

        // Phase 1: Queue the operation (MUST succeed - throws if fails)
        var operationId = await this.QueueDeleteOperationAsync(id, cancellationToken).ConfigureAwait(false);
        this._logger.LogDebug("Phase 1 complete: Operation {OperationId} queued for content {ContentId}", operationId, id);

        // Phase 2: Try to cancel ALL previous operations (best effort)
        try
        {
            await this.TryCancelAllOperationsAsync(id, operationId, cancellationToken).ConfigureAwait(false);
            this._logger.LogDebug("Phase 2 complete: Cancelled all previous operations for content {ContentId}", id);
        }
        catch (Exception ex)
        {
            // Best effort - log but don't fail
            this._logger.LogWarning(ex, "Phase 2 failed to cancel previous operations for content {ContentId} - continuing anyway", id);
        }

        // Processing: Try to process the new operation synchronously
        try
        {
            await this.TryProcessNextOperationAsync(id, cancellationToken).ConfigureAwait(false);
            this._logger.LogDebug("Processing complete for content {ContentId}", id);
            return WriteResult.Success(id);
        }
        catch (Exception ex)
        {
            // Log but don't fail - operation is queued and will be processed eventually
            this._logger.LogWarning(ex, "Failed to process operation synchronously for content {ContentId} - will be processed by background worker", id);
            return WriteResult.QueuedOnly(id, ex.Message);
        }
    }

    /// <summary>
    /// Retrieves content by ID.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    public async Task<ContentDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await this._context.Content
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);

        if (record == null)
        {
            return null;
        }

        return new ContentDto
        {
            Id = record.Id,
            Content = record.Content,
            MimeType = record.MimeType,
            ByteSize = record.ByteSize,
            ContentCreatedAt = record.ContentCreatedAt,
            RecordCreatedAt = record.RecordCreatedAt,
            RecordUpdatedAt = record.RecordUpdatedAt,
            Title = record.Title,
            Description = record.Description,
            Tags = record.Tags,
            Metadata = record.Metadata
        };
    }

    /// <summary>
    /// Counts total number of content records.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.Content.LongCountAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists content records with pagination support.
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of content DTOs.</returns>
    public async Task<List<ContentDto>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var records = await this._context.Content
            .AsNoTracking()
            .OrderByDescending(c => c.RecordCreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return records.Select(record => new ContentDto
        {
            Id = record.Id,
            Content = record.Content,
            MimeType = record.MimeType,
            ByteSize = record.ByteSize,
            ContentCreatedAt = record.ContentCreatedAt,
            RecordCreatedAt = record.RecordCreatedAt,
            RecordUpdatedAt = record.RecordUpdatedAt,
            Title = record.Title,
            Description = record.Description,
            Tags = record.Tags,
            Metadata = record.Metadata
        }).ToList();
    }

    // ========== Phase 1: Queue Operations (REQUIRED) ==========

    /// <summary>
    /// Phase 1: Queue an upsert operation. Must succeed.
    /// </summary>
    /// <param name="contentId"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    private async Task<string> QueueUpsertOperationAsync(string contentId, UpsertRequest request, CancellationToken cancellationToken)
    {
        // Build steps list - always upsert, then add index update for each search index
        var steps = new List<string> { "upsert" };

        // Add step for each configured search index using its ID
        foreach (var indexId in this._searchIndexById.Keys)
        {
            steps.Add($"index:{indexId}");
        }

        var operation = new OperationRecord
        {
            Id = this._cuidGenerator.Generate(),
            Complete = false,
            Cancelled = false,
            ContentId = contentId,
            Timestamp = DateTimeOffset.UtcNow,
            PlannedSteps = steps.ToArray(),
            CompletedSteps = [],
            RemainingSteps = steps.ToArray(),
            PayloadJson = JsonSerializer.Serialize(request),
            LastFailureReason = string.Empty,
            LastAttemptTimestamp = null
        };

        this._context.Operations.Add(operation);
        await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this._logger.LogDebug("Queued upsert operation {OperationId} for content {ContentId} with steps: {Steps}", operation.Id, contentId, string.Join(", ", steps));
        return operation.Id;
    }

    /// <summary>
    /// Phase 1: Queue a delete operation. Must succeed.
    /// </summary>
    /// <param name="contentId"></param>
    /// <param name="cancellationToken"></param>
    private async Task<string> QueueDeleteOperationAsync(string contentId, CancellationToken cancellationToken)
    {
        // Build steps list - always delete, then remove from each search index
        var steps = new List<string> { "delete" };

        // Add delete step for each configured search index using its ID
        foreach (var indexId in this._searchIndexById.Keys)
        {
            steps.Add($"index:{indexId}:delete");
        }

        var operation = new OperationRecord
        {
            Id = this._cuidGenerator.Generate(),
            Complete = false,
            Cancelled = false,
            ContentId = contentId,
            Timestamp = DateTimeOffset.UtcNow,
            PlannedSteps = steps.ToArray(),
            CompletedSteps = [],
            RemainingSteps = steps.ToArray(),
            PayloadJson = JsonSerializer.Serialize(new { Id = contentId }),
            LastFailureReason = string.Empty,
            LastAttemptTimestamp = null
        };

        this._context.Operations.Add(operation);
        await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this._logger.LogDebug("Queued delete operation {OperationId} for content {ContentId} with steps: {Steps}", operation.Id, contentId, string.Join(", ", steps));
        return operation.Id;
    }

    // ========== Phase 2: Optimize Queue (OPTIONAL - Best Effort) ==========

    /// <summary>
    /// Phase 2: Try to cancel superseded upsert operations (best effort).
    /// Only cancels incomplete Upsert operations older than the new one.
    /// Does NOT cancel Delete operations.
    /// </summary>
    /// <param name="contentId"></param>
    /// <param name="newOperationId"></param>
    /// <param name="cancellationToken"></param>
    private async Task TryCancelSupersededUpsertOperationsAsync(string contentId, string newOperationId, CancellationToken cancellationToken)
    {
        // Find incomplete operations with same ContentId and older Timestamp
        // Exclude Delete operations (they must complete)
        var timestamp = await this._context.Operations
            .Where(o => o.Id == newOperationId)
            .Select(o => o.Timestamp)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        var superseded = await this._context.Operations
            .Where(o => o.ContentId == contentId
                && o.Id != newOperationId
                && !o.Complete
                && o.Timestamp < timestamp
                && o.PlannedStepsJson.Contains("upsert")) // Only cancel upserts
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var op in superseded)
        {
            op.Cancelled = true;
            this._logger.LogDebug("Cancelled superseded operation {OperationId} for content {ContentId}", op.Id, contentId);
        }

        if (superseded.Count > 0)
        {
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Phase 2: Try to cancel ALL previous operations for delete (best effort).
    /// Cancels all incomplete operations older than the delete operation.
    /// </summary>
    /// <param name="contentId"></param>
    /// <param name="newOperationId"></param>
    /// <param name="cancellationToken"></param>
    private async Task TryCancelAllOperationsAsync(string contentId, string newOperationId, CancellationToken cancellationToken)
    {
        // Find incomplete operations with same ContentId and older Timestamp
        var timestamp = await this._context.Operations
            .Where(o => o.Id == newOperationId)
            .Select(o => o.Timestamp)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        var superseded = await this._context.Operations
            .Where(o => o.ContentId == contentId
                && o.Id != newOperationId
                && !o.Complete
                && o.Timestamp < timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var op in superseded)
        {
            op.Cancelled = true;
            this._logger.LogDebug("Cancelled operation {OperationId} for content {ContentId} due to delete", op.Id, contentId);
        }

        if (superseded.Count > 0)
        {
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // ========== Processing: Execute Operations ==========

    /// <summary>
    /// Try to process the next operation for a content ID.
    /// Skips locked operations (no recovery attempts).
    /// </summary>
    /// <param name="contentId"></param>
    /// <param name="cancellationToken"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catch all to ensure operation failure is logged and content remains locked for retry")]
    private async Task TryProcessNextOperationAsync(string contentId, CancellationToken cancellationToken)
    {
        // Step 1: Get next operation to process
        var operation = await this._context.Operations
            .Where(o => o.ContentId == contentId && !o.Complete)
            .OrderBy(o => o.Timestamp)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (operation == null)
        {
            this._logger.LogDebug("No operations to process for content {ContentId}", contentId);
            return;
        }

        // Check if operation is locked
        if (operation.LastAttemptTimestamp.HasValue)
        {
            this._logger.LogDebug("Operation {OperationId} is locked - skipping (no recovery)", operation.Id);
            return; // Skip locked operations
        }

        // If cancelled, mark complete and skip execution
        if (operation.Cancelled)
        {
            this._logger.LogDebug("Operation {OperationId} was cancelled - marking complete", operation.Id);
            operation.Complete = true;
            operation.LastFailureReason = "Cancelled";
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Try to process next operation recursively
            await this.TryProcessNextOperationAsync(contentId, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Step 2: Acquire lock (Transaction 1)
        var lockAcquired = await this.TryAcquireLockAsync(operation.Id, contentId, cancellationToken).ConfigureAwait(false);
        if (!lockAcquired)
        {
            this._logger.LogDebug("Failed to acquire lock for operation {OperationId} - another VM got there first", operation.Id);
            return; // Another VM got the lock
        }

        this._logger.LogDebug("Lock acquired for operation {OperationId}", operation.Id);

        try
        {
            // Step 3: Execute planned steps
            await this.ExecuteStepsAsync(operation, cancellationToken).ConfigureAwait(false);

            // Step 4: Complete and unlock (Transaction 2)
            await this.CompleteAndUnlockAsync(operation.Id, contentId, cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Operation {OperationId} completed successfully for content {ContentId}", operation.Id, contentId);

            // Step 5: Process next operation (if any)
            await this.TryProcessNextOperationAsync(contentId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Update failure reason
            operation.LastFailureReason = ex.Message;
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogError(ex, "Operation {OperationId} failed - content {ContentId} remains locked", operation.Id, contentId);
            throw; // Propagate error (operation and content remain locked)
        }
    }

    /// <summary>
    /// Step 2: Try to acquire lock on operation and content atomically.
    /// Uses raw SQL for atomic UPDATE with WHERE clause check.
    /// </summary>
    /// <param name="operationId"></param>
    /// <param name="contentId"></param>
    /// <param name="cancellationToken"></param>
    private async Task<bool> TryAcquireLockAsync(string operationId, string contentId, CancellationToken cancellationToken)
    {
        // Start a transaction for atomic lock acquisition
        using var transaction = await this._context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var now = DateTimeOffset.UtcNow.ToString("O"); // ISO 8601 format

            // Lock operation - only if LastAttemptTimestamp IS NULL
            const string OperationSql = @"
                UPDATE km_operations 
                SET LastAttemptTimestamp = @p0
                WHERE Id = @p1 
                  AND LastAttemptTimestamp IS NULL";

            var operationRows = await this._context.Database.ExecuteSqlRawAsync(
                OperationSql,
                [now, operationId],
                cancellationToken).ConfigureAwait(false);

            if (operationRows == 0)
            {
                // Failed to lock operation - already locked
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return false;
            }

            // Lock content - set Ready = false (only if Ready = true)
            const string ContentSql = @"
                UPDATE km_content 
                SET Ready = 0,
                    RecordUpdatedAt = @p0
                WHERE Id = @p1";

            // Note: We don't check Ready = true because content might not exist yet (insert case)
            // We execute this to ensure content is locked if it exists
            await this._context.Database.ExecuteSqlRawAsync(
                ContentSql,
                [now, contentId],
                cancellationToken).ConfigureAwait(false);

            // Commit transaction
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Step 3: Execute all remaining steps for an operation.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="cancellationToken"></param>
    private async Task ExecuteStepsAsync(OperationRecord operation, CancellationToken cancellationToken)
    {
        foreach (var step in operation.RemainingSteps)
        {
            this._logger.LogDebug("Executing step '{Step}' for operation {OperationId}", step, operation.Id);

            // Parse step name and execute
            if (step == "upsert")
            {
                await this.ExecuteUpsertStepAsync(operation, cancellationToken).ConfigureAwait(false);
            }
            else if (step == "delete")
            {
                await this.ExecuteDeleteStepAsync(operation, cancellationToken).ConfigureAwait(false);
            }
            else if (step.StartsWith("index:", StringComparison.Ordinal))
            {
                // Parse: "index:fts-stemmed" or "index:fts-exact:delete"
                var parts = step.Split(':');
                if (parts.Length < 2)
                {
                    throw new InvalidOperationException($"Invalid index step format: {step}");
                }

                var indexId = parts[1];
                var isDelete = parts.Length > 2 && parts[2] == "delete";

                if (isDelete)
                {
                    await this.ExecuteIndexDeleteStepAsync(operation, indexId, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await this.ExecuteIndexStepAsync(operation, indexId, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                throw new InvalidOperationException($"Unknown step type: {step}");
            }

            // Move step from Remaining to Completed
            var completed = operation.CompletedSteps.Concat([step]).ToArray();
            var remaining = operation.RemainingSteps.Where(s => s != step).ToArray();

            operation.CompletedSteps = completed;
            operation.RemainingSteps = remaining;

            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            this._logger.LogDebug("Step '{Step}' completed for operation {OperationId}", step, operation.Id);
        }
    }

    /// <summary>
    /// Execute upsert step: delete existing + create new (if exists).
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="cancellationToken"></param>
    private async Task ExecuteUpsertStepAsync(OperationRecord operation, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<UpsertRequest>(operation.PayloadJson)
            ?? throw new InvalidOperationException($"Failed to deserialize upsert payload for operation {operation.Id}");

        var now = DateTimeOffset.UtcNow;
        var contentCreatedAt = request.ContentCreatedAt ?? now;

        // Delete existing record if it exists
        var existing = await this._context.Content.FirstOrDefaultAsync(c => c.Id == operation.ContentId, cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            this._context.Content.Remove(existing);
            this._logger.LogDebug("Deleted existing content {ContentId} for upsert", operation.ContentId);
        }

        // Create new record
        var content = new ContentRecord
        {
            Id = operation.ContentId,
            Content = request.Content,
            MimeType = request.MimeType,
            ByteSize = Encoding.UTF8.GetByteCount(request.Content),
            Ready = false, // Will be set to true when operation completes
            ContentCreatedAt = contentCreatedAt,
            RecordCreatedAt = existing?.RecordCreatedAt ?? now, // Preserve original creation time if exists
            RecordUpdatedAt = now,
            Title = request.Title,
            Description = request.Description,
            Tags = request.Tags,
            Metadata = request.Metadata
        };

        this._context.Content.Add(content);
        await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this._logger.LogDebug("Created new content record {ContentId}", operation.ContentId);
    }

    /// <summary>
    /// Execute delete step: delete content if exists (idempotent).
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="cancellationToken"></param>
    private async Task ExecuteDeleteStepAsync(OperationRecord operation, CancellationToken cancellationToken)
    {
        var existing = await this._context.Content.FirstOrDefaultAsync(c => c.Id == operation.ContentId, cancellationToken).ConfigureAwait(false);

        if (existing != null)
        {
            this._context.Content.Remove(existing);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            this._logger.LogDebug("Deleted content {ContentId}", operation.ContentId);
        }
        else
        {
            this._logger.LogDebug("Content {ContentId} not found - delete is idempotent, no error", operation.ContentId);
        }
    }

    /// <summary>
    /// Execute index step: update content in specific search index.
    /// Throws if index ID not found in current configuration (operation remains locked for recovery).
    /// </summary>
    /// <param name="operation">The operation record.</param>
    /// <param name="indexId">The search index identifier (from config).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ExecuteIndexStepAsync(OperationRecord operation, string indexId, CancellationToken cancellationToken)
    {
        // Fail hard if index ID not found in current config
        if (!this._searchIndexById.TryGetValue(indexId, out var searchIndex))
        {
            this._logger.LogError("Search index '{IndexId}' not found in current configuration for operation {OperationId}. Operation will remain locked until index is restored or manually recovered.", indexId, operation.Id);
            throw new InvalidOperationException($"Search index '{indexId}' not found in current configuration. Cannot process operation {operation.Id}.");
        }

        // Get the content from the database
        var content = await this._context.Content
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == operation.ContentId, cancellationToken)
            .ConfigureAwait(false);

        if (content == null)
        {
            this._logger.LogWarning("Content {ContentId} not found for indexing in index {IndexId} - skipping", operation.ContentId, indexId);
            return;
        }

        // Update the search index
        await searchIndex.IndexAsync(operation.ContentId, content.Content, cancellationToken).ConfigureAwait(false);
        this._logger.LogDebug("Indexed content {ContentId} in search index {IndexId}", operation.ContentId, indexId);
    }

    /// <summary>
    /// Execute index delete step: remove content from specific search index.
    /// Throws if index ID not found in current configuration (operation remains locked for recovery).
    /// </summary>
    /// <param name="operation">The operation record.</param>
    /// <param name="indexId">The search index identifier (from config).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ExecuteIndexDeleteStepAsync(OperationRecord operation, string indexId, CancellationToken cancellationToken)
    {
        // Fail hard if index ID not found in current config
        if (!this._searchIndexById.TryGetValue(indexId, out var searchIndex))
        {
            this._logger.LogError("Search index '{IndexId}' not found in current configuration for operation {OperationId}. Operation will remain locked until index is restored or manually recovered.", indexId, operation.Id);
            throw new InvalidOperationException($"Search index '{indexId}' not found in current configuration. Cannot process operation {operation.Id}.");
        }

        // Remove from search index (idempotent)
        await searchIndex.RemoveAsync(operation.ContentId, cancellationToken).ConfigureAwait(false);
        this._logger.LogDebug("Removed content {ContentId} from search index {IndexId}", operation.ContentId, indexId);
    }

    /// <summary>
    /// Step 4: Complete operation and unlock content.
    /// </summary>
    /// <param name="operationId"></param>
    /// <param name="contentId"></param>
    /// <param name="cancellationToken"></param>
    private async Task CompleteAndUnlockAsync(string operationId, string contentId, CancellationToken cancellationToken)
    {
        using var transaction = await this._context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var now = DateTimeOffset.UtcNow.ToString("O");

            // Mark operation complete
            const string OperationSql = @"
                UPDATE km_operations
                SET Complete = 1
                WHERE Id = @p0";

            await this._context.Database.ExecuteSqlRawAsync(OperationSql, [operationId], cancellationToken).ConfigureAwait(false);

            // Unlock content (set Ready = true)
            const string ContentSql = @"
                UPDATE km_content
                SET Ready = 1,
                    RecordUpdatedAt = @p0
                WHERE Id = @p1";

            await this._context.Database.ExecuteSqlRawAsync(ContentSql, [now, contentId], cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            this._logger.LogDebug("Operation {OperationId} completed and content {ContentId} unlocked", operationId, contentId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
