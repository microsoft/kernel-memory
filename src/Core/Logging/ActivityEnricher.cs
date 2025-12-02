// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace KernelMemory.Core.Logging;

/// <summary>
/// Serilog enricher that adds correlation IDs from System.Diagnostics.Activity.
/// Provides TraceId and SpanId properties for distributed tracing and log correlation.
/// </summary>
public sealed class ActivityEnricher : ILogEventEnricher
{
    /// <summary>
    /// Property name for the trace ID (from Activity.TraceId).
    /// </summary>
    public const string TraceIdPropertyName = "TraceId";

    /// <summary>
    /// Property name for the span ID (from Activity.SpanId).
    /// </summary>
    public const string SpanIdPropertyName = "SpanId";

    /// <summary>
    /// Enriches the log event with Activity correlation IDs if available.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">Factory for creating log event properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity == null)
        {
            return;
        }

        // Add TraceId for correlating logs across the entire operation
        var traceId = activity.TraceId.ToString();
        if (!string.IsNullOrEmpty(traceId) && traceId != LoggingConstants.EmptyTraceId)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(TraceIdPropertyName, traceId));
        }

        // Add SpanId for correlating logs within a specific span
        var spanId = activity.SpanId.ToString();
        if (!string.IsNullOrEmpty(spanId) && spanId != LoggingConstants.EmptySpanId)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(SpanIdPropertyName, spanId));
        }
    }
}
