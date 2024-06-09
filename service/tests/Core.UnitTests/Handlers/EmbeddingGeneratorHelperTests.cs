// Copyright (c) Microsoft. All rights reserved.

using Microsoft.IdentityModel.Tokens;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Handlers;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.SemanticKernel.Embeddings;
using Moq;

namespace Microsoft.KM.Core.UnitTests.Handlers;

public class EmbeddingGeneratorHelperTests
{
    [Theory]
    [InlineData(DataPipeline.ArtifactTypes.TextPartition, 1)]
    [InlineData(DataPipeline.ArtifactTypes.SyntheticData, 1)]
    [InlineData(DataPipeline.ArtifactTypes.ExtractedContent, 0)]
    public async Task GetEmbeddingsCorrectlyCheckForArtifactType(
        DataPipeline.ArtifactTypes artifactType,
        int expectedCallCount)
    {
        // Arrange
        var pipelineMock = new DataPipeline();
        var pipelineStepHandlerMock = new Mock<IPipelineStepHandler>();
        var orchestratorMock = new Mock<IPipelineOrchestrator>();
        var embeddingGeneratorMock = new Mock<ITextEmbeddingGenerator>();
        var embeddingGenerators = new List<ITextEmbeddingGenerator> { embeddingGeneratorMock.Object };
        var cancellationToken = CancellationToken.None;

        var generatedFileDetails = new DataPipeline.GeneratedFileDetails
        {
            ArtifactType = artifactType,
            MimeType = MimeTypes.PlainText
        };

        var generatedFiles = new Dictionary<string, DataPipeline.GeneratedFileDetails>
            {
                { "file1", generatedFileDetails }
            };

        //only some artifact type should generate the embedding
        var fileDetails = new DataPipeline.FileDetails()
        {
            ArtifactType = artifactType,
            GeneratedFiles = generatedFiles
        };
        pipelineMock.Files.Add(fileDetails);

        orchestratorMock.Setup(o => o.ReadTextFileAsync(It.IsAny<DataPipeline>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("sample text");

        embeddingGeneratorMock.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Embedding());

        // Act
        var result = await EmbeddingGeneratorHelper.GetEmbeddingsAsync(
            pipelineMock,
            pipelineStepHandlerMock.Object,
            embeddingGenerators,
            orchestratorMock.Object,
            cancellationToken);

        // Assert
        Assert.NotNull(result);
        embeddingGeneratorMock.Verify(e => e.GenerateEmbeddingAsync("sample text", It.IsAny<CancellationToken>()), Times.Exactly(expectedCallCount));
    }

    [Fact]
    public async Task BatchEmbeddingIsUsedWhenPossible()
    {
        // Arrange
        var pipelineMock = new DataPipeline();
        var pipelineStepHandlerMock = new Mock<IPipelineStepHandler>();
        var orchestratorMock = new Mock<IPipelineOrchestrator>();
        var embeddingGeneratorMock = new Mock<IEmbeddingCombined>();
        var embeddingGenerators = new List<ITextEmbeddingGenerator> { embeddingGeneratorMock.Object };
        var cancellationToken = CancellationToken.None;

        var generatedFileDetails = new DataPipeline.GeneratedFileDetails
        {
            ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
            MimeType = MimeTypes.PlainText
        };

        var generatedFiles = new Dictionary<string, DataPipeline.GeneratedFileDetails>
            {
                { "file1", generatedFileDetails },
                { "file2", generatedFileDetails },
                { "file3", generatedFileDetails },
            };

        //only some artifact type should generate the embedding
        var fileDetails = new DataPipeline.FileDetails()
        {
            ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
            GeneratedFiles = generatedFiles
        };
        pipelineMock.Files.Add(fileDetails);

        orchestratorMock.Setup(o => o.ReadTextFileAsync(It.IsAny<DataPipeline>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("sample text");

        embeddingGeneratorMock.Setup(e => e.GenerateEmbeddingBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Embedding(), new Embedding(), new Embedding()]);
        embeddingGeneratorMock.Setup(e => e.MaxTokens).Returns(1000);
        embeddingGeneratorMock.Setup(e => e.CountTokens(It.IsAny<string>())).Returns(10);
        embeddingGeneratorMock.Setup(e => e.EmbeddingBatchMaxSize).Returns(10);

        // Act
        var result = await EmbeddingGeneratorHelper.GetEmbeddingsAsync(
            pipelineMock,
            pipelineStepHandlerMock.Object,
            embeddingGenerators,
            orchestratorMock.Object,
            cancellationToken);

        // Assert
        string[] expected = ["sample text", "sample text", "sample text"];
        Assert.NotNull(result);

        embeddingGeneratorMock.Verify(e => e.GenerateEmbeddingBatchAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()),
            Times.Exactly(1));
    }

    [Fact]
    public async Task BatchEmbeddingHonorsMaxNumberOfBatchElements()
    {
        // Arrange
        var pipelineMock = new DataPipeline();
        var pipelineStepHandlerMock = new Mock<IPipelineStepHandler>();
        var orchestratorMock = new Mock<IPipelineOrchestrator>();
        var embeddingGeneratorMock = new Mock<IEmbeddingCombined>();
        var embeddingGenerators = new List<ITextEmbeddingGenerator> { embeddingGeneratorMock.Object };
        var cancellationToken = CancellationToken.None;

        var generatedFileDetails = new DataPipeline.GeneratedFileDetails
        {
            ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
            MimeType = MimeTypes.PlainText
        };

        var generatedFiles = new Dictionary<string, DataPipeline.GeneratedFileDetails>
            {
                { "file1", generatedFileDetails },
                { "file2", generatedFileDetails },
                { "file3", generatedFileDetails },
            };

        //only some artifact type should generate the embedding
        var fileDetails = new DataPipeline.FileDetails()
        {
            ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
            GeneratedFiles = generatedFiles
        };
        pipelineMock.Files.Add(fileDetails);

        orchestratorMock.Setup(o => o.ReadTextFileAsync(It.IsAny<DataPipeline>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("sample text");

        embeddingGeneratorMock.Setup(e => e.GenerateEmbeddingBatchAsync(It.Is<IEnumerable<string>>(e => e.Count() == 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Embedding(), new Embedding()]);

        embeddingGeneratorMock.Setup(e => e.GenerateEmbeddingBatchAsync(It.Is<IEnumerable<string>>(e => e.Count() == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Embedding()]);

        embeddingGeneratorMock.Setup(e => e.MaxTokens).Returns(1000);
        embeddingGeneratorMock.Setup(e => e.CountTokens(It.IsAny<string>())).Returns(10);

        //important: this setup maximum number of element equal to 2.
        embeddingGeneratorMock.Setup(e => e.EmbeddingBatchMaxSize).Returns(2);

        // Act
        var result = await EmbeddingGeneratorHelper.GetEmbeddingsAsync(
            pipelineMock,
            pipelineStepHandlerMock.Object,
            embeddingGenerators,
            orchestratorMock.Object,
            cancellationToken);

        // Assert
        string[] expected = ["sample text", "sample text", "sample text"];
        Assert.NotNull(result);

        embeddingGeneratorMock.Verify(e => e.GenerateEmbeddingBatchAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task BatchEmbeddingHonorsMaxNumberOfTokens()
    {
        // Arrange
        var pipelineMock = new DataPipeline();
        var pipelineStepHandlerMock = new Mock<IPipelineStepHandler>();
        var orchestratorMock = new Mock<IPipelineOrchestrator>();
        var embeddingGeneratorMock = new Mock<IEmbeddingCombined>();
        var embeddingGenerators = new List<ITextEmbeddingGenerator> { embeddingGeneratorMock.Object };
        var cancellationToken = CancellationToken.None;

        var generatedFileDetails = new DataPipeline.GeneratedFileDetails
        {
            ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
            MimeType = MimeTypes.PlainText
        };

        var generatedFiles = new Dictionary<string, DataPipeline.GeneratedFileDetails>
            {
                { "file1", generatedFileDetails },
                { "file2", generatedFileDetails },
                { "file3", generatedFileDetails },
            };

        //only some artifact type should generate the embedding
        var fileDetails = new DataPipeline.FileDetails()
        {
            ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
            GeneratedFiles = generatedFiles
        };
        pipelineMock.Files.Add(fileDetails);

        orchestratorMock.Setup(o => o.ReadTextFileAsync(It.IsAny<DataPipeline>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("sample text");

        embeddingGeneratorMock.Setup(e => e.GenerateEmbeddingBatchAsync(It.Is<IEnumerable<string>>(e => e.Count() == 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Embedding(), new Embedding()]);

        embeddingGeneratorMock.Setup(e => e.GenerateEmbeddingBatchAsync(It.Is<IEnumerable<string>>(e => e.Count() == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Embedding()]);

        //important: this setup maximum number of tokens to 25, since we count 10 tokens each string, we will have two call , the first one with 20
        //tokens and the other with 10 tokens.
        embeddingGeneratorMock.Setup(e => e.MaxTokens).Returns(25);
        embeddingGeneratorMock.Setup(e => e.CountTokens(It.IsAny<string>())).Returns(10);

        embeddingGeneratorMock.Setup(e => e.EmbeddingBatchMaxSize).Returns(200);

        // Act
        var result = await EmbeddingGeneratorHelper.GetEmbeddingsAsync(
            pipelineMock,
            pipelineStepHandlerMock.Object,
            embeddingGenerators,
            orchestratorMock.Object,
            cancellationToken);

        // Assert
        string[] expected = ["sample text", "sample text", "sample text"];
        Assert.NotNull(result);

        embeddingGeneratorMock.Verify(e => e.GenerateEmbeddingBatchAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    public interface IEmbeddingCombined : ITextEmbeddingGenerator, ITextEmbeddingBatchGenerator;
}
