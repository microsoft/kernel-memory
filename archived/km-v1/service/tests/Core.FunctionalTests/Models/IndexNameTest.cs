// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Models;

namespace Microsoft.KM.Core.FunctionalTests.Models;

public class IndexNameTest
{
    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(null, "abc", "abc")]
    [InlineData("", "bcd", "bcd")]
    [InlineData("   ", "cde", "cde")]
    [InlineData("   ", " def  ", "def")]
    [InlineData(" \n  ", "cde", "cde")]
    [InlineData(" \r  ", " def  ", "def")]
    [InlineData(" \t  ", " def  ", "def")]
    [InlineData("123", null, "123")]
    [InlineData("123", "", "123")]
    [InlineData("234", "xyz", "234")]
    [InlineData(" 345    ", "xyz", "345")]
    [InlineData(" 456    ", "    xyz    ", "456")]
    public void ItReturnsExpectedIndexName(string? name, string defaultName, string expected)
    {
        Assert.Equal(expected, IndexName.CleanName(name, defaultName));
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(null, null)]
    [InlineData(null, "")]
    [InlineData("", null)]
    [InlineData("", "")]
    [InlineData("     ", "")]
    [InlineData("", "     ")]
    [InlineData("   ", "   ")]
    [InlineData(" \n  ", "   ")]
    [InlineData(" ", " \n  ")]
    [InlineData(" \r  ", "   ")]
    [InlineData(" ", " \r  ")]
    [InlineData(" \t  ", "   ")]
    [InlineData(" ", " \t  ")]
    public void ItThrowsIfIndexNameCannotBeCalculated(string? name, string defaultName)
    {
        Assert.Throws<ArgumentNullException>(() => IndexName.CleanName(name, defaultName));
    }
}
