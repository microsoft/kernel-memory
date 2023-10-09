// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Models;

namespace UnitTests.Models;

public class FileCollectionTest
{
    [Fact]
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

    [Fact]
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
        target.AddStream(file0.Name, new FileStream(file0.FullName, FileMode.Open));
        target.AddStream(file1.Name, new FileStream(file1.FullName, FileMode.Open));
        target.AddStream(file2.Name, new FileStream(file2.FullName, FileMode.Open));
        target.AddStream(file0.Name, new FileStream(file0.FullName, FileMode.Open));
        target.AddStream(file1.Name, new FileStream(file1.FullName, FileMode.Open));
        target.AddStream(file2.Name, new FileStream(file2.FullName, FileMode.Open));

        // Assert
        Assert.Equal(6, target.GetStreams().ToList().Count);
    }
}
