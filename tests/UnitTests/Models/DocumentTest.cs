// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;

namespace UnitTests.Models;

public class DocumentTest
{
    [Fact]
    public void ItReplacesSpecialChars()
    {
        // Assert - special chars
        Assert.Equal("a_b.txt", Document.FsNameToId("a b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a/b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a:b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a;b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a,b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a~b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a!b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a?b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a@b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a#b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a$b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a%b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a^b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a&b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a*b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a+b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a-b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a=b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a'b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a`b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a_b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId("a|b.txt"));

        // Assert - empty and escaped chars
        Assert.Equal("a_b.txt", Document.FsNameToId("a\nb.txt"));
        Assert.Equal("a_nb.txt", Document.FsNameToId(@"a\nb.txt"));

        Assert.Equal("a_b.txt", Document.FsNameToId("a\rb.txt"));
        Assert.Equal("a_rb.txt", Document.FsNameToId(@"a\rb.txt"));

        Assert.Equal("a_b.txt", Document.FsNameToId("a\tb.txt"));
        Assert.Equal("a_tb.txt", Document.FsNameToId(@"a\tb.txt"));

        Assert.Equal("a_b.txt", Document.FsNameToId("a\vb.txt"));
        Assert.Equal("a_vb.txt", Document.FsNameToId(@"a\vb.txt"));

        Assert.Equal("a_b.txt", Document.FsNameToId("a\\b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId(@"a\\b.txt"));

        Assert.Equal("a_b.txt", Document.FsNameToId("a\"b.txt"));
        Assert.Equal("a_b.txt", Document.FsNameToId(@"a""b.txt"));

        Assert.Equal("a_b.txt", Document.FsNameToId("a\0b.txt"));
        Assert.Equal("a_0b.txt", Document.FsNameToId(@"a\0b.txt"));

        // Assert - Dirs
        Assert.Equal("c_dir_file.txt", Document.FsNameToId(@"c:\dir\file.txt"));
        Assert.Equal("c_dir_file.txt", Document.FsNameToId(@"c:/dir/file.txt"));
        Assert.Equal("dir_file.txt", Document.FsNameToId(@"dir/file.txt"));
        Assert.Equal("dir_file.txt", Document.FsNameToId(@"/dir/file.txt"));
        Assert.Equal("dir_file.txt", Document.FsNameToId(@"/dir/file.txt/"));

        // Assert - Trimming
        Assert.Equal("a_b.txt", Document.FsNameToId("-a\0b.txt-"));

        // Assert - Duplicates
        Assert.Equal("a_a_.txt", Document.FsNameToId("-a-+-a-.txt=="));
    }
}
