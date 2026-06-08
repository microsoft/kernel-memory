// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.Enums;

namespace KernelMemory.Core.Tests.Config;

/// <summary>
/// Tests for CacheModes enum to verify all expected values exist.
/// These values control cache read/write behavior for embedding caching.
/// </summary>
public sealed class CacheModesTests
{
    [Fact]
    public void CacheModes_ShouldHaveReadWriteValue()
    {
        // Assert
        Assert.True(Enum.IsDefined(CacheModes.ReadWrite));
    }

    [Fact]
    public void CacheModes_ShouldHaveReadOnlyValue()
    {
        // Assert
        Assert.True(Enum.IsDefined(CacheModes.ReadOnly));
    }

    [Fact]
    public void CacheModes_ShouldHaveWriteOnlyValue()
    {
        // Assert
        Assert.True(Enum.IsDefined(CacheModes.WriteOnly));
    }

    [Fact]
    public void CacheModes_ShouldHaveExactlyThreeValues()
    {
        // Assert
        var values = Enum.GetValues<CacheModes>();
        Assert.Equal(3, values.Length);
    }

    [Theory]
    [InlineData("ReadWrite", CacheModes.ReadWrite)]
    [InlineData("ReadOnly", CacheModes.ReadOnly)]
    [InlineData("WriteOnly", CacheModes.WriteOnly)]
    public void CacheModes_ShouldParseFromString(string name, CacheModes expected)
    {
        // Act
        var parsed = Enum.Parse<CacheModes>(name);

        // Assert
        Assert.Equal(expected, parsed);
    }
}
