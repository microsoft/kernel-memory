// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Text;

namespace Microsoft.KM.Abstractions.UnitTests.Text;

public class StringExtensionsTest
{
    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData("\n", "\n")]
    [InlineData("\r", "\n")] // Old Mac
    [InlineData("\r\n", "\n")] // Windows
    [InlineData("\n\r", "\n\n")] // Not standard, that's 2 line endings
    [InlineData("\n\n\n", "\n\n\n")]
    [InlineData("\r\r\r", "\n\n\n")]
    [InlineData("\r\r\n\r", "\n\n\n")]
    [InlineData("\n\r\n\r", "\n\n\n")]
    [InlineData("ciao", "ciao")]
    [InlineData("ciao ", "ciao ")]
    [InlineData(" ciao ", " ciao ")]
    [InlineData("\r ciao ", "\n ciao ")]
    [InlineData(" \rciao ", " \nciao ")]
    [InlineData(" \r\nciao ", " \nciao ")]
    [InlineData(" \r\nciao\n ", " \nciao\n ")]
    [InlineData(" \r\nciao \n", " \nciao \n")]
    [InlineData(" \r\nciao \r", " \nciao \n")]
    [InlineData(" \r\nciao \rn", " \nciao \nn")]
    public void ItNormalizesLineEndings(string? input, string? expected)
    {
        // Act
#pragma warning disable CS8604 // it's an extension method, internally it handles the null scenario
        string actual = input.NormalizeNewlines();
#pragma warning restore CS8604

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("\n", "")]
    [InlineData("\r", "")]
    [InlineData("\r\n", "")]
    [InlineData("\n\r", "")]
    [InlineData("\n\n\n", "")]
    [InlineData("\r\r\r", "")]
    [InlineData("\r\r\n\r", "")]
    [InlineData("\n\r\n\r", "")]
    [InlineData("ciao", "ciao")]
    [InlineData("ciao ", "ciao")]
    [InlineData(" ciao ", "ciao")]
    [InlineData("\r ciao ", "ciao")]
    [InlineData(" \rciao ", "ciao")]
    [InlineData(" \r\nciao ", "ciao")]
    [InlineData(" \r\nciao\n ", "ciao")]
    [InlineData(" \r\nciao \n", "ciao")]
    [InlineData(" \r\nciao \r", "ciao")]
    [InlineData(" \r\nciao \rn", "ciao \nn")]
    [InlineData(" \r\nc\ri\ra\no \r", "c\ni\na\no")]
    [InlineData(" \r\nc\r\ni\n\na\r\ro \r", "c\ni\n\na\n\no")]
    [InlineData(" \r\nccc\r\ni\n\naaa\r\ro \r", "ccc\ni\n\naaa\n\no")]
    public void ItCanTrimWhileNormalizingLineEndings(string? input, string? expected)
    {
        // Act
#pragma warning disable CS8604 // it's an extension method, internally it handles the null scenario
        string actual = input.NormalizeNewlines(true);
#pragma warning restore CS8604

        // Assert
        Assert.Equal(expected, actual);
    }
}
