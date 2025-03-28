// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KM.TestHelpers;

namespace Microsoft.KM.Core.UnitTests.FileSystem.DevTools;

public class InMemoryFileSystemTest : BaseUnitTestCase
{
    private readonly VolatileFileSystem _target;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BinaryData>> _internalState;

    public InMemoryFileSystemTest(ITestOutputHelper output) : base(output)
    {
        this._target = new VolatileFileSystem();
        this._internalState = this._target.GetInternalState();
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItIsolatesDirectories()
    {
        // Arrange
        var instance1 = VolatileFileSystem.GetInstance("dir1");
        var instance2 = VolatileFileSystem.GetInstance("dir2");

        // Act
        await instance1.CreateVolumeAsync("volume1");
        await instance1.CreateDirectoryAsync("volume1", "path1");
        await instance1.CreateDirectoryAsync("volume1", "path2");

        await instance1.CreateVolumeAsync("volume2");
        await instance1.CreateDirectoryAsync("volume2", "path3");

        await instance2.CreateVolumeAsync("volume1");
        await instance2.CreateDirectoryAsync("volume1", "path4");

        await instance2.CreateVolumeAsync("volume3");
        await instance2.CreateVolumeAsync("volume4");

        // Assert
        var instance1Volumes = (await instance1.ListVolumesAsync()).ToList();
        var instance2Volumes = (await instance2.ListVolumesAsync()).ToList();

        Assert.Equal(2, instance1Volumes.Count);
        Assert.Equal(3, instance2Volumes.Count);

        Assert.True(instance1Volumes.Contains("volume1"));
        Assert.True(instance1Volumes.Contains("volume2"));
        Assert.False(instance1Volumes.Contains("volume3"));
        Assert.False(instance1Volumes.Contains("volume4"));

        Assert.True(instance2Volumes.Contains("volume1"));
        Assert.False(instance2Volumes.Contains("volume2"));
        Assert.True(instance2Volumes.Contains("volume3"));
        Assert.True(instance2Volumes.Contains("volume4"));
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItCreatesAndDeletesVolumes()
    {
        // Arrange
        var volume = "testVolume1";

        // Act
        await this._target.CreateVolumeAsync(volume);

        // Assert
        Assert.True(this._internalState.ContainsKey(volume));

        // Act
        await this._target.DeleteVolumeAsync(volume);

        // Assert
        Assert.False(this._internalState.ContainsKey(volume));
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItChecksIfVolumesExists()
    {
        // Arrange
        var volume = "testVolume2";

        // Act
        await this._target.CreateVolumeAsync(volume);

        // Assert
        Assert.True(await this._target.VolumeExistsAsync(volume));

        // Cleanup
        await this._target.DeleteVolumeAsync(volume);
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
        Assert.True(this._internalState.ContainsKey(vol));
        Assert.True(this._internalState[vol].ContainsKey("sub1/sub2/"));
        await this._target.DeleteVolumeAsync(vol);

        // Assert
        Assert.False(this._internalState.ContainsKey(vol));
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
        Assert.True(this._internalState[Vol].ContainsKey("sub1/sub2/"));

        // Act
        await this._target.DeleteDirectoryAsync(Vol, "sub1/sub2");

        // Assert
        Assert.False(this._internalState[Vol].ContainsKey("sub1/sub2/"));

        // Act
        await this._target.DeleteDirectoryAsync(Vol, "sub1");

        // Assert
        Assert.False(this._internalState[Vol].ContainsKey("sub1/sub2/"));

        // Cleanup
        await this._target.DeleteVolumeAsync(Vol);
        Assert.False(this._internalState.ContainsKey(Vol));
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
