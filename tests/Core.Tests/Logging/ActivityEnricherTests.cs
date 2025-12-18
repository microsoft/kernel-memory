// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using Serilog.Events;

namespace KernelMemory.Core.Tests.Logging;

/// <summary>
/// Tests for ActivityEnricher - validates correlation ID enrichment from System.Diagnostics.Activity.
/// Activity correlation is critical for distributed tracing and log correlation.
/// </summary>
public sealed class ActivityEnricherTests : IDisposable
{
    private Activity? _activity;

    /// <summary>
    /// Cleans up activity after each test.
    /// </summary>
    public void Dispose()
    {
        this._activity?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies TraceId property name constant is defined.
    /// </summary>
    [Fact]
    public void TraceIdPropertyName_ShouldBeDefined()
    {
        // Assert
        Assert.Equal("TraceId", ActivityEnricher.TraceIdPropertyName);
    }

    /// <summary>
    /// Verifies SpanId property name constant is defined.
    /// </summary>
    [Fact]
    public void SpanIdPropertyName_ShouldBeDefined()
    {
        // Assert
        Assert.Equal("SpanId", ActivityEnricher.SpanIdPropertyName);
    }

    /// <summary>
    /// Verifies Enrich does nothing when Activity.Current is null.
    /// </summary>
    [Fact]
    public void Enrich_WhenNoActivity_ShouldNotAddProperties()
    {
        // Arrange
        Activity.Current = null;
        var enricher = new ActivityEnricher();
        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        enricher.Enrich(logEvent, propertyFactory);

        // Assert - no properties should be added
        Assert.Empty(logEvent.Properties);
    }

    /// <summary>
    /// Verifies Enrich adds TraceId and SpanId when Activity is present.
    /// </summary>
    [Fact]
    public void Enrich_WhenActivityPresent_ShouldAddTraceAndSpanIds()
    {
        // Arrange
        this._activity = new Activity("TestOperation");
        this._activity.Start();
        var enricher = new ActivityEnricher();
        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        enricher.Enrich(logEvent, propertyFactory);

        // Assert - both TraceId and SpanId should be added
        Assert.True(logEvent.Properties.ContainsKey(ActivityEnricher.TraceIdPropertyName));
        Assert.True(logEvent.Properties.ContainsKey(ActivityEnricher.SpanIdPropertyName));
    }

    /// <summary>
    /// Verifies TraceId property has correct value from Activity.
    /// </summary>
    [Fact]
    public void Enrich_ShouldSetCorrectTraceId()
    {
        // Arrange
        this._activity = new Activity("TestOperation");
        this._activity.Start();
        var enricher = new ActivityEnricher();
        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        enricher.Enrich(logEvent, propertyFactory);

        // Assert
        var traceIdProperty = logEvent.Properties[ActivityEnricher.TraceIdPropertyName] as ScalarValue;
        Assert.NotNull(traceIdProperty);
        Assert.Equal(this._activity.TraceId.ToString(), traceIdProperty.Value);
    }

    /// <summary>
    /// Verifies SpanId property has correct value from Activity.
    /// </summary>
    [Fact]
    public void Enrich_ShouldSetCorrectSpanId()
    {
        // Arrange
        this._activity = new Activity("TestOperation");
        this._activity.Start();
        var enricher = new ActivityEnricher();
        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        enricher.Enrich(logEvent, propertyFactory);

        // Assert
        var spanIdProperty = logEvent.Properties[ActivityEnricher.SpanIdPropertyName] as ScalarValue;
        Assert.NotNull(spanIdProperty);
        Assert.Equal(this._activity.SpanId.ToString(), spanIdProperty.Value);
    }

    /// <summary>
    /// Verifies existing properties are not overwritten.
    /// </summary>
    [Fact]
    public void Enrich_WhenPropertyExists_ShouldNotOverwrite()
    {
        // Arrange
        this._activity = new Activity("TestOperation");
        this._activity.Start();
        var enricher = new ActivityEnricher();
        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Pre-add a TraceId property
        const string existingTraceId = "existing-trace-id";
        logEvent.AddPropertyIfAbsent(new LogEventProperty(ActivityEnricher.TraceIdPropertyName, new ScalarValue(existingTraceId)));

        // Act
        enricher.Enrich(logEvent, propertyFactory);

        // Assert - existing property should be preserved
        var traceIdProperty = logEvent.Properties[ActivityEnricher.TraceIdPropertyName] as ScalarValue;
        Assert.NotNull(traceIdProperty);
        Assert.Equal(existingTraceId, traceIdProperty.Value);
    }

    /// <summary>
    /// Creates a test log event for enrichment testing.
    /// </summary>
    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test message", []),
            []);
    }

    /// <summary>
    /// Test implementation of ILogEventPropertyFactory for unit testing.
    /// </summary>
    private sealed class TestPropertyFactory : Serilog.Core.ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
