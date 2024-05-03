// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Models;

namespace Microsoft.KM.Abstractions.UnitTests.Models;

public class FileCollectionTest
{
    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItDeDupesCheckingThePath()
    {
        // Arrange
        var file0 = new FileInfo("Fixtures/Doc1.txt");
        var file1 = new FileInfo("Fixtures/Doc1.txt");
        var file2 = new FileInfo("Fixtures/Documents/Doc1.txt");
        Assert.True(file0.Exists);
        Assert.True(file1.Exists);
        Assert.True(file2.Exists);

        // Act
        var target = new FileCollection();
        target.AddFile(file0.FullName);
        target.AddFile(file1.FullName);
        target.AddFile(file2.FullName);
        target.AddFile(file0.FullName);
        target.AddFile(file1.FullName);
        target.AddFile(file2.FullName);

        // Assert
        Assert.Equal(2, target.GetStreams().ToList().Count);
    }

#if !OS_WINDOWS // does not allow 2 open streams to the same file
    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItDoesntDeDupeStreams()
    {
        // Arrange
        var file0 = new FileInfo("Fixtures/Doc1.txt");
        var file1 = new FileInfo("Fixtures/Doc1.txt");
        var file2 = new FileInfo("Fixtures/Documents/Doc1.txt");
        Assert.True(file0.Exists);
        Assert.True(file1.Exists);
        Assert.True(file2.Exists);

        // Act
        var target = new FileCollection();

        using var f01 = new FileStream(file0.FullName, FileMode.Open);
        target.AddStream(file0.Name, f01);
        using var f11 = new FileStream(file1.FullName, FileMode.Open);
        target.AddStream(file0.Name, f01);
        using var f21 = new FileStream(file2.FullName, FileMode.Open);
        target.AddStream(file0.Name, f01);

        using var f02 = new FileStream(file0.FullName, FileMode.Open);
        target.AddStream(file0.Name, f01);
        using var f12 = new FileStream(file1.FullName, FileMode.Open);
        target.AddStream(file0.Name, f01);
        using var f22 = new FileStream(file2.FullName, FileMode.Open);
        target.AddStream(file0.Name, f01);

        target.AddStream(file0.Name, f01);
        target.AddStream(file1.Name, f12);
        target.AddStream(file2.Name, f22);

        // Assert
        Assert.Equal(9, target.GetStreams().ToList().Count);
    }
#endif
}
