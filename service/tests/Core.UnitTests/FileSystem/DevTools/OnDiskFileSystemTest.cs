// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KM.TestHelpers;
using Xunit.Abstractions;

namespace Microsoft.KM.Core.UnitTests.FileSystem.DevTools;

public class OnDiskFileSystemTest : BaseUnitTestCase
{
    private const string RootDir = "volumes";
    private readonly DiskFileSystem _target;

    public OnDiskFileSystemTest(ITestOutputHelper output) : base(output)
    {
        this._target = new DiskFileSystem(RootDir);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItCreatesAndDeletesVolumes()
    {
        // Act
        await this._target.CreateVolumeAsync("testVolume1");

        // Assert
        Assert.True(Directory.Exists(Path.Join(RootDir, "testVolume1")));

        // Act
        await this._target.DeleteVolumeAsync("testVolume1");

        // Assert
        Assert.False(Directory.Exists(Path.Join(RootDir, "testVolume1")));
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItChecksIfVolumesExists()
    {
        // Act
        await this._target.CreateVolumeAsync("testVolume2");

        // Assert
        Assert.True(await this._target.VolumeExistsAsync("testVolume2"));

        // Cleanup
        await this._target.DeleteVolumeAsync("testVolume2");
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItDeletesNonEmptyVolumes()
    {
        // Arrange
        string vol = Guid.NewGuid().ToString();
        await this._target.CreateVolumeAsync(vol);
        await this._target.CreateDirectoryAsync(vol, "sub1/sub2");
        await this._target.WriteFileAsync(vol, "sub1/sub2", "file.txt", "some content");

        // Act
        Assert.True(Directory.Exists(Path.Join(RootDir, vol)));
        Assert.True(Directory.Exists(Path.Join(RootDir, vol, "sub1")));
        await this._target.DeleteVolumeAsync(vol);

        // Assert
        Assert.False(Directory.Exists(Path.Join(RootDir, "v3")));
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItDeletesDirectories()
    {
        // Arrange
        const string Vol = "v3";
        await this._target.CreateVolumeAsync(Vol);
        await this._target.CreateDirectoryAsync(Vol, "sub1/sub2");
        await this._target.WriteFileAsync(Vol, "sub1/sub2", "file.txt", "some content");
        Assert.True(Directory.Exists(Path.Join(RootDir, Vol, "sub1")));

        // Act
        await this._target.DeleteDirectoryAsync(Vol, "sub1/sub2");

        // Assert
        Assert.True(Directory.Exists(Path.Join(RootDir, Vol, "sub1")));

        // Act
        await this._target.DeleteDirectoryAsync(Vol, "sub1");

        // Assert
        Assert.False(Directory.Exists(Path.Join(RootDir, Vol, "sub1")));

        // Cleanup
        await this._target.DeleteVolumeAsync(Vol);
        Assert.False(Directory.Exists(Path.Join(RootDir, Vol)));
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItChecksIfFileExists()
    {
        // Arrange
        const string Vol = "v4";
        await this._target.CreateVolumeAsync(Vol);
        await this._target.CreateDirectoryAsync(Vol, "sub1/sub2");
        await this._target.WriteFileAsync(Vol, "sub1/sub2", "file.txt", "some content");

        // Act - Assert
        Assert.False(await this._target.FileExistsAsync(Vol, "sub1", ""));
        Assert.False(await this._target.FileExistsAsync(Vol, "sub1", "file"));
        Assert.False(await this._target.FileExistsAsync(Vol, "sub1/sub2", ""));
        Assert.False(await this._target.FileExistsAsync(Vol, "sub1/sub2", "file"));
        Assert.True(await this._target.FileExistsAsync(Vol, "sub1/sub2", "file.txt"));

        // Cleanup
        await this._target.DeleteVolumeAsync(Vol);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItReadsFilesThatExist()
    {
        // Arrange
        const string Vol = "v5";
        await this._target.CreateVolumeAsync(Vol);
        await this._target.CreateDirectoryAsync(Vol, "sub1/sub2");
        await this._target.WriteFileAsync(Vol, "sub1/sub2", "file.txt", "some content");

        // Act
        var content = await this._target.ReadFileAsTextAsync(Vol, "sub1/sub2", "file.txt");

        // Assert
        Assert.Equal("some content", content);

        // Cleanup
        await this._target.DeleteVolumeAsync(Vol);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItStreamsFilesThatExist()
    {
        // Arrange
        const string Vol = "v5";
        await this._target.CreateVolumeAsync(Vol);
        await this._target.CreateDirectoryAsync(Vol, "sub1/sub2");
        await this._target.WriteFileAsync(Vol, "sub1/sub2", "file.txt", "some content");

        // Act
        var contentFile = await this._target.ReadFileInfoAsync(Vol, "sub1/sub2", "file.txt");
        BinaryData? data;
        await using (Stream stream = await contentFile.GetStreamAsync())
        {
            data = new BinaryData(stream.ReadAllBytes());
            stream.Close();
        }

        var content = data.ToString();

        // Assert
        Assert.Equal("some content", content);

        // Cleanup
        await this._target.DeleteVolumeAsync(Vol);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItThrowsIfAFileOrDirDoesntExist()
    {
        // Arrange
        const string Vol = "v6";
        await this._target.CreateVolumeAsync(Vol);
        await this._target.CreateDirectoryAsync(Vol, "sub1/sub2");

        // Act - Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            async () => await this._target.ReadFileAsTextAsync(Vol, "foo/bar", "file.txt"));
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await this._target.ReadFileAsTextAsync(Vol, "sub1/sub2", "file.txt"));

        // Cleanup
        await this._target.DeleteVolumeAsync(Vol);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItCanListFiles()
    {
        // Arrange
        const string Vol = "v7";
        await this._target.CreateVolumeAsync(Vol);
        await this._target.CreateDirectoryAsync(Vol, "sub1/sub2");
        await this._target.WriteFileAsync(Vol, "sub1", "file1.txt", "some content 1");
        await this._target.WriteFileAsync(Vol, "sub1/sub2", "file2.txt", "some content 2");
        await this._target.WriteFileAsync(Vol, "sub1/sub2", "file3.txt", "some content 3");

        // Act (sort where needed to avoid false negatives)
        var list1 = await this._target.GetAllFileNamesAsync(Vol, "");
        var list2 = await this._target.GetAllFileNamesAsync(Vol, "sub1");
        var list3 = (await this._target.GetAllFileNamesAsync(Vol, "sub1/sub2")).ToImmutableSortedSet();

        // Assert (note: if these fails, there's a chance your OS is injecting temp files like .DS_Store on disk)
        Assert.Equal(0, list1.Count());
        Assert.Equal(1, list2.Count());
        Assert.Equal(2, list3.Count);

        // Note: the file name must not have elements of the path
        Assert.Equal("file1.txt", list2.ElementAt(0));
        Assert.Equal("file2.txt", list3.ElementAt(0));
        Assert.Equal("file3.txt", list3.ElementAt(1));

        // Cleanup
        await this._target.DeleteVolumeAsync(Vol);
    }

#pragma warning disable CA5394

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItDoesntCorruptBinaryFiles()
    {
        // Arrange
        const string Vol = "v8";
        await this._target.CreateVolumeAsync(Vol);
        var originalContent = new byte[100];
        Random rnd = new();
        int originalHash = 0;
        for (int i = 0; i < originalContent.Length; i++)
        {
            var byteVal = rnd.Next(256);
            originalHash += byteVal;
            originalContent[i] = Convert.ToByte(byteVal);
        }

        // Act
        await this._target.WriteFileAsync(Vol, "", "file.bin", new BinaryData(originalContent).ToStream());
        var savedContent = (await this._target.ReadFileAsBinaryAsync(Vol, "", "file.bin")).ToArray();

        // Assert
        Console.WriteLine($"File sizes: {savedContent.Length} == {originalContent.Length}");
        Assert.Equal(originalContent.Length, savedContent.Length);
        var savedHash = 0;
        for (int i = 0; i < savedContent.Length; i++)
        {
            savedHash += savedContent[i];
        }

        Console.WriteLine($"File content hashes: {originalHash} == {savedHash}");
        Assert.Equal(originalHash, savedHash);
    }

#pragma warning restore CA5394

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItDoesntCorruptTextFiles()
    {
        // Arrange
        const string Vol = "v9";
        await this._target.CreateVolumeAsync(Vol);
        string originalContent = JsonSerializer.Serialize(new { foo = "bar", bar = new { baz = "♣︎♣︎♣︎♣︎" } });

        // Act
        await this._target.WriteFileAsync(Vol, "", "file.json", new BinaryData(originalContent).ToStream());
        char[] savedContent = (await this._target.ReadFileAsTextAsync(Vol, "", "file.json")).ToArray();
        var saveContentStr = new string(savedContent);

        // Assert
        Console.WriteLine($"File sizes: {savedContent.Length} == {originalContent.Length}");
        Assert.Equal(originalContent.Length, savedContent.Length);
        Assert.Equal(originalContent, saveContentStr);
    }
}
