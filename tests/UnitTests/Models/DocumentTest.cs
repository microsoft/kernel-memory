// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

namespace UnitTests.Models;

public class DocumentTest
{
    [Fact]
    public void ItReplacesSpecialChars()
    {
        // Assert - No exception occurs
        Assert.Equal("a-b.txt", Document.ValidateId("a-b.txt"));
        Assert.Equal("a_b.txt", Document.ValidateId("a_b.txt"));

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
