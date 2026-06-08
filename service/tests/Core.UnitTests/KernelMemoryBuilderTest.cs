// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Pipeline.Queue;
using Microsoft.KM.TestHelpers;
using Moq;

namespace Microsoft.KM.Core.UnitTests;

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
        var myDocumentStorage = new Mock<IDocumentStorage>();
        var myMimeTypeDetection = new Mock<IMimeTypeDetection>();
        var myTextEmbeddingGenerator = new Mock<ITextEmbeddingGenerator>();
        var myTextGenerator = new Mock<ITextGenerator>();
        var myMemoryDb = new Mock<IMemoryDb>();

        myTextEmbeddingGenerator.SetupGet(x => x.MaxTokens).Returns(int.MaxValue);

        var target = new KernelMemoryBuilder()
            .WithCustomDocumentStorage(myDocumentStorage.Object)
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
        var myDocumentStorage = new Mock<IDocumentStorage>();
        var myMimeTypeDetection = new Mock<IMimeTypeDetection>();
        var myTextEmbeddingGenerator = new Mock<ITextEmbeddingGenerator>();
        var myTextGenerator = new Mock<ITextGenerator>();
        var myMemoryDb = new Mock<IMemoryDb>();

        myTextEmbeddingGenerator.SetupGet(x => x.MaxTokens).Returns(int.MaxValue);

        var target = new KernelMemoryBuilder(hostServiceCollection)
            .WithCustomIngestionQueueClientFactory(myQueueFactory)
            .WithCustomDocumentStorage(myDocumentStorage.Object)
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
        var myDocumentStorage = new Mock<IDocumentStorage>();
        var myMimeTypeDetection = new Mock<IMimeTypeDetection>();
        var myTextGenerator = new Mock<ITextGenerator>();
        var myMemoryDb = new Mock<IMemoryDb>();

        var target = new KernelMemoryBuilder()
            .WithCustomDocumentStorage(myDocumentStorage.Object)
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
        Assert.Contains("some dependencies are not defined", e.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Embedding generator", e.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItCanMixPersistentAndVolatileStorageIfNeeded()
    {
        // Arrange
        var kmbOptions = KernelMemoryBuilderBuildOptions.WithVolatileAndPersistentData;

        // Act - Assert no exception occurs
        new KernelMemoryBuilder()
            .WithOpenAIDefaults("key")
            .WithSimpleFileStorage(SimpleFileStorageConfig.Volatile)
            .WithPostgresMemoryDb("Host=localhost;Port=5432;Username=public;Password=;Database=public")
            .Build<MemoryServerless>(kmbOptions);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItCanMixPersistentAndVolatileStorageIfNeeded2()
    {
        // Arrange
        var kmbOptions = KernelMemoryBuilderBuildOptions.WithVolatileAndPersistentData;
        var serviceCollection1 = new ServiceCollection();
        var serviceCollection2 = new ServiceCollection();

        // Act - Assert no exception occurs
        serviceCollection1.AddKernelMemory(builder =>
        {
            builder
                .WithOpenAIDefaults("key")
                .WithSimpleFileStorage(SimpleFileStorageConfig.Volatile)
                .WithPostgresMemoryDb("Host=localhost;Port=5432;Username=public;Password=;Database=public");
        }, kmbOptions);

        serviceCollection2.AddKernelMemory<MemoryServerless>(builder =>
        {
            builder
                .WithOpenAIDefaults("key")
                .WithSimpleFileStorage(SimpleFileStorageConfig.Volatile)
                .WithPostgresMemoryDb("Host=localhost;Port=5432;Username=public;Password=;Database=public");
        }, kmbOptions);
    }
}
