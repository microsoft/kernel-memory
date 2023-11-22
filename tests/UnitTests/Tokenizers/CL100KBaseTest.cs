// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI.Tokenizers.CL100KBase;
using UnitTests.TestHelpers;
using Xunit.Abstractions;

namespace UnitTests.Tokenizers;

// ReSharper disable StringLiteralTypo
public class CL100KBaseTest : BaseTestCase
{
    public CL100KBaseTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void ItTokenizesTextConsistently()
    {
        // Act
        var tokens1 = CL100KBaseTokenizer.Encode("Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
        Console.WriteLine(tokens1);

        var tokens2 = CL100KBaseTokenizer.Encode("Nam vel dignissim est, et vestibulum velit.");
        Console.WriteLine(tokens2);

        var tokens3 = CL100KBaseTokenizer.Encode(string.Empty);
        Console.WriteLine(tokens3);

        // Assert
        Assert.Equal(new() { 33883, 27439, 24578, 2503, 28311, 11, 36240, 59024, 31160, 13 }, tokens1);
        Assert.Equal(new() { 72467, 9231, 28677, 1056, 318, 1826, 11, 1880, 92034, 16903, 72648, 13 }, tokens2);
        Assert.Equal(new(), tokens3);
    }
}
