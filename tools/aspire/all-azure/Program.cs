// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Safety.AzureAIContentSafety;
using Projects;

namespace Microsoft.KernelMemory.Aspire.AppHost;

internal static class Program
{
    private static readonly AzureAIContentSafetyConfig s_azureAIContentSafetyConfig = new();
    private static readonly AzureAIDocIntelConfig s_azureAIDocIntelConfig = new();
    private static readonly AzureAISearchConfig s_azureAISearchConfig = new();
    private static readonly AzureBlobsConfig s_azureBlobsConfig = new();
    private static readonly AzureOpenAIConfig s_azureOpenAIEmbeddingConfig = new();
    private static readonly AzureOpenAIConfig s_azureOpenAITextConfig = new();
    private static readonly AzureQueuesConfig s_azureQueuesConfig = new();

    internal static void Main()
    {
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:AzureAIContentSafety", s_azureAIContentSafetyConfig)
            .BindSection("KernelMemory:Services:AzureAIDocIntel", s_azureAIDocIntelConfig)
            .BindSection("KernelMemory:Services:AzureAISearch", s_azureAISearchConfig)
            .BindSection("KernelMemory:Services:AzureBlobs", s_azureBlobsConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", s_azureOpenAIEmbeddingConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIText", s_azureOpenAITextConfig)
            .BindSection("KernelMemory:Services:AzureQueues", s_azureQueuesConfig);

        RunFromCode();
    }

    private static void RunFromCode()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddProject<Service>("kernel-memory")
            // .WithHttpEndpoint(targetPort: 20001)
            // .WithEnvironment("Kestrel__Endpoints__Http__Url", "http://*:20001")
            .WithKmTextEmbeddingGenerationEnvironment("AzureOpenAIEmbedding", s_azureOpenAIEmbeddingConfig)
            .WithKmTextGenerationEnvironment("AzureOpenAIText", s_azureOpenAITextConfig)
            .WithKmMemoryDbEnvironment("AzureAISearch", s_azureAISearchConfig)
            .WithKmOrchestrationEnvironment("AzureQueues", s_azureQueuesConfig)
            .WithKmDocumentStorageEnvironment("AzureBlobs", s_azureBlobsConfig)
            .WithKMContentSafetyModerationEnvironment("AzureAIContentSafety", s_azureAIContentSafetyConfig)
            .WithKmOcrEnvironment("AzureAIDocIntel", s_azureAIDocIntelConfig)
            // Other KM settings
            .WithEnvironment("KernelMemory__Service__OpenApiEnabled", "True");

        builder.Build().Run();
    }
}
