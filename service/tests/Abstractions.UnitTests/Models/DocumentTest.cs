// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

namespace Abstractions.UnitTests.Models;

public class DocumentTest
{
    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItReplacesSpecialChars()
    {
        // Assert - No exception occurs
        Assert.Equal("a-b.txt", Document.ValidateId("a-b.txt"));
        Assert.Equal("a_b.txt", Document.ValidateId("a_b.txt"));
        Assert.Equal("abcdefghijklmnopqrstuvwxyz", Document.ValidateId("abcdefghijklmnopqrstuvwxyz"));
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZ", Document.ValidateId("ABCDEFGHIJKLMNOPQRSTUVWXYZ"));
        Assert.Equal("01234567890", Document.ValidateId("01234567890"));
        Assert.Equal("-_.", Document.ValidateId("-_."));

        // Assert - Empty strings
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(""));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(null));

        // Assert - special chars
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a/b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a:b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a;b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a,b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a~b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a!b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a?b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a@b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a#b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a$b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a%b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a^b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a&b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a*b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a+b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a=b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a'b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a`b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a|b.txt"));

        // Assert - empty and escaped chars
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a\nb.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"a\nb.txt"));

        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a\rb.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"a\rb.txt"));

        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a\tb.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"a\tb.txt"));

        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a\vb.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"a\vb.txt"));

        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a\\b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"a\\b.txt"));

        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a\"b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"a""b.txt"));

        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("a\0b.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"a\0b.txt"));

        // Assert - Dirs
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"c:\dir\file.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"c:/dir/file.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"dir/file.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"/dir/file.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId(@"/dir/file.txt/"));

        // Assert - Trimming
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("-a\0b.txt-"));

        // Assert - Duplicates
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.ValidateId("-a-+-a-.txt=="));
    }
}
