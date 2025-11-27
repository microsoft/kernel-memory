using System.Text.Json;

namespace KernelMemory.Core.Storage.Entities;

/// <summary>
/// Entity representing an operation in the Operations table.
/// Used for queue-based processing with distributed locking.
/// </summary>
public class OperationRecord
{
    public string Id { get; set; } = string.Empty;
    public bool Complete { get; set; }
    public bool Cancelled { get; set; }
    public string ContentId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string LastFailureReason { get; set; } = string.Empty;

    /// <summary>
    /// When last attempt was made (nullable). Used for distributed locking.
    /// If NOT NULL and Complete=false: operation is locked (executing or crashed).
    /// </summary>
    public DateTimeOffset? LastAttemptTimestamp { get; set; }

    // JSON-backed array fields
    public string PlannedStepsJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the planned steps array. Not mapped to database - uses PlannedStepsJson for persistence.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] PlannedSteps
    {
        get => string.IsNullOrWhiteSpace(this.PlannedStepsJson)
            ? []
            : JsonSerializer.Deserialize<string[]>(this.PlannedStepsJson) ?? [];
        set => this.PlannedStepsJson = JsonSerializer.Serialize(value);
    }

    public string CompletedStepsJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the completed steps array. Not mapped to database - uses CompletedStepsJson for persistence.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] CompletedSteps
    {
        get => string.IsNullOrWhiteSpace(this.CompletedStepsJson)
            ? []
            : JsonSerializer.Deserialize<string[]>(this.CompletedStepsJson) ?? [];
        set => this.CompletedStepsJson = JsonSerializer.Serialize(value);
    }

    public string RemainingStepsJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the remaining steps array. Not mapped to database - uses RemainingStepsJson for persistence.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] RemainingSteps
    {
        get => string.IsNullOrWhiteSpace(this.RemainingStepsJson)
            ? []
            : JsonSerializer.Deserialize<string[]>(this.RemainingStepsJson) ?? [];
        set => this.RemainingStepsJson = JsonSerializer.Serialize(value);
    }

    // Payload stored as JSON
    public string PayloadJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the payload object. Not mapped to database - uses PayloadJson for persistence.
    /// </summary>
    public object? Payload
    {
        get => string.IsNullOrWhiteSpace(this.PayloadJson)
            ? null
            : JsonSerializer.Deserialize<object>(this.PayloadJson);
        set => this.PayloadJson = value == null ? "{}" : JsonSerializer.Serialize(value);
    }
}
