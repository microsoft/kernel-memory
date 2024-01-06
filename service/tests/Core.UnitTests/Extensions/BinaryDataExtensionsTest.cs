// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Extensions;
using Microsoft.TestHelpers;
using Xunit.Abstractions;

namespace Core.UnitTests.Extensions;

// ReSharper disable StringLiteralTypo
public class BinaryDataExtensionsTest : BaseUnitTestCase
{
    public BinaryDataExtensionsTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItHashesDataConsistently()
    {
        // Act
        var hash1 = new BinaryData("24362 34 dsf.gs/.df gsdfg sd").CalculateSHA256();
        Console.WriteLine(hash1);

        var hash2 = new BinaryData("DGFHE35§£¢∞§#V%EFH F 2463456......").CalculateSHA256();
        Console.WriteLine(hash2);

        var hash3 = new BinaryData("").CalculateSHA256();
        Console.WriteLine(hash3);

        // Assert
        Assert.Equal("ffa7270c6d29bc7c76b82e2c6c0c94741d09db242245ecc63aa3824bac79b9b6", hash1);
        Assert.Equal("789ecf31d032c493a98bd83c1ac53ae3f93d0dd530e2a4495aeb911f72b487b0", hash2);
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", hash3);
    }
}
