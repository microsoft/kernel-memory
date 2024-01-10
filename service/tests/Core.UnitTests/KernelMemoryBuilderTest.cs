// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Pipeline.Queue;
using Microsoft.TestHelpers;
using Moq;
using Xunit.Abstractions;

namespace Core.UnitTests;

public class KernelMemoryBuilderTest : BaseUnitTestCase
{
    public KernelMemoryBuilderTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItBuildsServerlessClients()
    {
        // Arrange
        var myContentStorage = new Mock<IContentStorage>();
        var myMimeTypeDetection = new Mock<IMimeTypeDetection>();
        var myTextEmbeddingGenerator = new Mock<ITextEmbeddingGenerator>();
        var myTextGenerator = new Mock<ITextGenerator>();
        var myMemoryDb = new Mock<IMemoryDb>();

        myTextEmbeddingGenerator.SetupGet(x => x.MaxTokens).Returns(int.MaxValue);

        var target = new KernelMemoryBuilder()
            .WithCustomStorage(myContentStorage.Object)
            .WithCustomMimeTypeDetection(myMimeTypeDetection.Object)
            .WithCustomEmbeddingGenerator(myTextEmbeddingGenerator.Object)
            .WithCustomTextGenerator(myTextGenerator.Object)
            .WithCustomMemoryDb(myMemoryDb.Object);

        // Act
        var memory = target.Build();

        // Assert
        Assert.True(memory is MemoryServerless);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItBuildsAsyncClients()
    {
        // Arrange
        var hostServiceCollection = new ServiceCollection();
        var myQueue = new Mock<IQueue>();
        var myQueueFactory = new QueueClientFactory(() => myQueue.Object);
        var myContentStorage = new Mock<IContentStorage>();
        var myMimeTypeDetection = new Mock<IMimeTypeDetection>();
        var myTextEmbeddingGenerator = new Mock<ITextEmbeddingGenerator>();
        var myTextGenerator = new Mock<ITextGenerator>();
        var myMemoryDb = new Mock<IMemoryDb>();

        myTextEmbeddingGenerator.SetupGet(x => x.MaxTokens).Returns(int.MaxValue);

        var target = new KernelMemoryBuilder(hostServiceCollection)
            .WithCustomIngestionQueueClientFactory(myQueueFactory)
            .WithCustomStorage(myContentStorage.Object)
            .WithCustomMimeTypeDetection(myMimeTypeDetection.Object)
            .WithCustomEmbeddingGenerator(myTextEmbeddingGenerator.Object)
            .WithCustomTextGenerator(myTextGenerator.Object)
            .WithCustomMemoryDb(myMemoryDb.Object);

        // Act
        var memory = target.Build();

        // Assert
        Assert.True(memory is MemoryService);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItDetectsMissingEmbeddingGenerator()
    {
        // Arrange
        var myContentStorage = new Mock<IContentStorage>();
        var myMimeTypeDetection = new Mock<IMimeTypeDetection>();
        var myTextGenerator = new Mock<ITextGenerator>();
        var myMemoryDb = new Mock<IMemoryDb>();

        var target = new KernelMemoryBuilder()
            .WithCustomStorage(myContentStorage.Object)
            .WithCustomMimeTypeDetection(myMimeTypeDetection.Object)
            .WithCustomTextGenerator(myTextGenerator.Object)
            .WithCustomMemoryDb(myMemoryDb.Object);

        // Act + Assert
        Exception? e = null;
        try
        {
            target.Build();
            Assert.Fail($"The code should throw a {nameof(ConfigurationException)} because the embedding generator is not defined");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception message: " + ex.Message);
            e = ex;
        }

        // Assert - See KernelMemoryBuilder.GetBuildType()
        Assert.True(e is ConfigurationException);
        Assert.Contains("some dependencies are not defined", e.Message);
        Assert.Contains("Embedding generator", e.Message);
    }
}
