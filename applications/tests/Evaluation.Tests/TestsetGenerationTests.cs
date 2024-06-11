﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Evaluation;
using Microsoft.KernelMemory.Evaluation.TestSet;
using Microsoft.SemanticKernel;
using Xunit.Abstractions;

namespace Microsoft.KM.Evaluation.FunctionalTests;

#pragma warning disable SKEXP0010
public class TestsetGenerationTests
{
    private readonly IKernelMemory _memory;
    private readonly TestSetGenerator _testSetGenerator;
    private readonly TestSetEvaluator _testSetEvaluator;
    private readonly Kernel _kernel;

    public TestsetGenerationTests(IConfiguration cfg, ITestOutputHelper output)
    {
        var azureOpenAITextConfig = new AzureOpenAIConfig();
        var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();

        cfg
            .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig);

        var memoryBuilder = new KernelMemoryBuilder()
            .With(new KernelMemoryConfig { DefaultIndexName = "default4tests" })
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig, new DefaultGPTTokenizer())
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig, new DefaultGPTTokenizer());

        this._kernel = Kernel
            .CreateBuilder()
            .AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: azureOpenAIEmbeddingConfig.Deployment,
                endpoint: azureOpenAIEmbeddingConfig.Endpoint,
                apiKey: azureOpenAIEmbeddingConfig.APIKey)
            .AddAzureOpenAIChatCompletion(
                deploymentName: azureOpenAITextConfig.Deployment,
                endpoint: azureOpenAITextConfig.Endpoint,
                apiKey: azureOpenAITextConfig.APIKey)
            .Build();

        this._testSetGenerator = new TestSetGeneratorBuilder(memoryBuilder.Services)
            .AddEvaluatorKernel(this._kernel)
            .Build();

        this._memory = memoryBuilder.Build();

        this._testSetEvaluator = new TestSetEvaluatorBuilder()
            .AddEvaluatorKernel(this._kernel)
            .WithMemory(this._memory)
            .Build();
    }

    [Fact]
    [Trait("Category", "Evaluation")]
    public async Task ItGenerateTestSetAsync()
    {
        await this._memory
            .ImportDocumentAsync(
                "file1-NASA-news.pdf",
                documentId: "file1-NASA-news",
                steps: Constants.PipelineWithoutSummary);

        var testSets = await this._testSetGenerator.GenerateTestSetsAsync("default4tests", retryCount: 5, count: 1)
            .ToArrayAsync();

        Assert.NotEmpty(testSets);
        Assert.Equal(1, testSets.Length);
    }

    [Fact]
    [Trait("Category", "Evaluation")]
    public async Task ItEvaluateTestSetAsync()
    {
        await this._memory
            .ImportDocumentAsync(
                "file1-NASA-news.pdf",
                documentId: "file1-NASA-news",
                steps: Constants.PipelineWithoutSummary);

        var evaluation = await this._testSetEvaluator.EvaluateTestSetAsync("default4tests", new[]
        {
            new TestSetItem
            {
                Question = "What is the role of the Department of Defense in the recovery operations for the Artemis II mission?",
                GroundTruth = "The Department of Defense personnel are involved in practicing recovery operations for the Artemis II mission. They use a crew module test article to help verify the recovery team's readiness to recover the Artemis II crew and the Orion spacecraft.",
            }
        }).ToArrayAsync();

        Assert.NotEmpty(evaluation);
    }
}
