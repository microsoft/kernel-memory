// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.KernelMemory.MemoryStorage;
using UnitTests.TestHelpers;
using Xunit.Abstractions;

namespace UnitTests.MemoryStorage;

public class MemoryRecordTest : BaseTestCase
{
    public MemoryRecordTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void ItCanBeSerialized()
    {
        // Arrange
        var record = new MemoryRecord();

        // Act
        string serialized = JsonSerializer.Serialize(record);
        var actual = JsonSerializer.Deserialize<MemoryRecord>(serialized);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(record.Id, actual.Id);
        Assert.Equal(JsonSerializer.Serialize(record.Vector), JsonSerializer.Serialize(actual.Vector));

        // Arrange
        record.Id = "123";
        record.Tags["foo"] = new List<string?> { "bar" };
        record.Payload["bar"] = "foo";

        // Act
        serialized = JsonSerializer.Serialize(record);
        actual = JsonSerializer.Deserialize<MemoryRecord>(serialized);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal("123", actual.Id);
        Assert.Equal("bar", actual.Tags["foo"].First());
        Assert.Equal("foo", actual.Payload["bar"].ToString());
    }

    [Fact]
    public void ItSupportsSchemaVersioning()
    {
        // This constant should never change
        Assert.Equal("schema", Microsoft.KernelMemory.Constants.ReservedPayloadSchemaVersionField);

        // Arrange
        var record = new MemoryRecord();
        Assert.True(record.Payload.ContainsKey("schema"));

        // Act
        string serialized = JsonSerializer.Serialize(record);
        var actual = JsonSerializer.Deserialize<MemoryRecord>(serialized);

        // Assert
        Assert.NotNull(actual);
        Assert.True(actual.Payload.ContainsKey("schema"));
        Assert.Equal("20231218A", record.Payload["schema"]);
    }

    [Fact]
    public void ItSelfUpgrades()
    {
        // Arrange
        var record1 = new MemoryRecord();
        var record2 = new MemoryRecord();

        // Act
        record1.Payload.Remove("schema");
        record2.Payload["schema"] = "";

        // Assert
        Assert.Equal("20231218A", record1.Payload["schema"]);
        Assert.Equal("20231218A", record2.Payload["schema"]);

        // Act
        record1.Payload.Remove(Microsoft.KernelMemory.Constants.ReservedPayloadUrlField);
        record2.Payload[Microsoft.KernelMemory.Constants.ReservedPayloadUrlField] = "foo";
        record1.Payload.Remove("schema");
        record2.Payload.Remove("schema");

        // Assert - the default value is added
        Assert.Equal("", record1.Payload[Microsoft.KernelMemory.Constants.ReservedPayloadUrlField]);

        // Assert - the value persists even if an upgrade occurred
        Assert.Equal("foo", record2.Payload[Microsoft.KernelMemory.Constants.ReservedPayloadUrlField]);
    }

    [Fact]
    public void ItAllowsToRemoveKeys()
    {
        // Arrange
        var record1 = new MemoryRecord();
        var record2 = new MemoryRecord();

        // Act - Note: the Url value is removed after the internal upgrade occurs
        record1.Payload.Remove("schema");
        record1.Payload.Remove(Microsoft.KernelMemory.Constants.ReservedPayloadUrlField);

        // Assert - The upgrade should not occur hence the key should not exist
        Assert.False(record1.Payload.ContainsKey(Microsoft.KernelMemory.Constants.ReservedPayloadUrlField));
    }
}
