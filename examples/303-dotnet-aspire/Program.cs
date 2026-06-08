// Copyright (c) Microsoft. All rights reserved.

using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Aspire;
using Projects;

internal static class Program
{
    private const string QdrantImage = "qdrant/qdrant";
    private const string KMDockerImage = "kernelmemory/service";

    private static readonly AzureOpenAIConfig s_azureOpenAIEmbeddingConfig = new();
    private static readonly AzureOpenAIConfig s_azureOpenAITextConfig = new();
    private static readonly OpenAIConfig s_openAIConfig = new();

    internal static void Main()
    {
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:OpenAI", s_openAIConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", s_azureOpenAIEmbeddingConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIText", s_azureOpenAITextConfig);

        RunFromCode();

        // RunFromDockerWithOpenAI(s_openAIConfig.APIKey);

        // RunFromDockerImage("latest");
    }

    private static void RunFromCode()
    {
        var builder = DistributedApplication.CreateBuilder();

        var qdrant = builder.AddContainer("qdrant", QdrantImage)
            .WithHttpEndpoint(targetPort: 6333)
            .PublishAsContainer();

        // Find Qdrant endpoint and pass the value to KM below
        var qdrantEndpoint = qdrant.GetEndpoint("http");

        builder.AddProject<Service>("kernel-memory")
            // Wait for Qdrant to be ready
            .WaitFor(qdrant)
            // Global KM settings
            .WithEnvironment("KernelMemory__TextGeneratorType", "AzureOpenAIText")
            .WithEnvironment("KernelMemory__DataIngestion__EmbeddingGeneratorTypes__0", "AzureOpenAIEmbedding")
            .WithEnvironment("KernelMemory__DataIngestion__MemoryDbTypes__0", "Qdrant")
            .WithEnvironment("KernelMemory__Retrieval__EmbeddingGeneratorType", "AzureOpenAIEmbedding")
            .WithEnvironment("KernelMemory__Retrieval__MemoryDbType", "Qdrant")
            // Qdrant settings
            .WithEnvironment("KernelMemory__Services__Qdrant__Endpoint", qdrantEndpoint)
            .WithEnvironment("KernelMemory__Services__Qdrant__APIKey", "")
            // Azure OpenAI settings - Text generation
            .WithEnvironment("KernelMemory__Services__AzureOpenAIText__Auth", s_azureOpenAITextConfig.Auth.ToString("G"))
            .WithEnvironment("KernelMemory__Services__AzureOpenAIText__APIKey", s_azureOpenAITextConfig.APIKey)
            .WithEnvironment("KernelMemory__Services__AzureOpenAIText__Endpoint", s_azureOpenAITextConfig.Endpoint)
            .WithEnvironment("KernelMemory__Services__AzureOpenAIText__Deployment", s_azureOpenAITextConfig.Deployment)
            // Azure OpenAI settings - Embeddings
            .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__Auth", s_azureOpenAIEmbeddingConfig.Auth.ToString("G"))
            .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__APIKey", s_azureOpenAIEmbeddingConfig.APIKey)
            .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__Endpoint", s_azureOpenAIEmbeddingConfig.Endpoint)
            .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__Deployment", s_azureOpenAIEmbeddingConfig.Deployment);

        builder
            .ShowDashboardUrl()
            .LaunchDashboard()
            .Build().Run();
    }

    private static void RunFromDockerWithOpenAI(string openAIKey, string dockerTag = "latest")
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddContainer("kernel-memory", KMDockerImage, tag: dockerTag)
            .WithEnvironment("KernelMemory__TextGeneratorType", "OpenAI")
            .WithEnvironment("KernelMemory__DataIngestion__EmbeddingGeneratorTypes__0", "OpenAI")
            .WithEnvironment("KernelMemory__Retrieval__EmbeddingGeneratorType", "OpenAI")
            .WithEnvironment("KernelMemory__Services__OpenAI__APIKey", openAIKey);

        builder.Build().Run();
    }

    private static void RunFromDockerImage(string dockerTag = "latest")
    {
        var builder = DistributedApplication.CreateBuilder();

        var qdrant = builder.AddContainer("qdrant", QdrantImage)
            .WithHttpEndpoint(targetPort: 6333)
            .PublishAsContainer();

        // Find Qdrant endpoint and pass the value to KM below
        var qdrantEndpoint = qdrant.GetEndpoint("http");

        builder.AddContainer("kernel-memory", KMDockerImage, tag: dockerTag)
            .WithHttpEndpoint(targetPort: 9001)
            // Wait for Qdrant to be ready
            .WaitFor(qdrant)
            // Global KM settings
            .WithEnvironment("KernelMemory__TextGeneratorType", "AzureOpenAIText")
            .WithEnvironment("KernelMemory__DataIngestion__EmbeddingGeneratorTypes__0", "AzureOpenAIEmbedding")
            .WithEnvironment("KernelMemory__DataIngestion__MemoryDbTypes__0", "Qdrant")
            .WithEnvironment("KernelMemory__Retrieval__EmbeddingGeneratorType", "AzureOpenAIEmbedding")
            .WithEnvironment("KernelMemory__Retrieval__MemoryDbType", "Qdrant")
            // Qdrant settings
            .WithEnvironment("KernelMemory__Services__Qdrant__Endpoint", qdrantEndpoint)
            .WithEnvironment("KernelMemory__Services__Qdrant__APIKey", "")
            // Azure OpenAI settings - Text generation
            .WithEnvironment("KernelMemory__Services__AzureOpenAIText__Auth", s_azureOpenAITextConfig.Auth.ToString("G"))
            .WithEnvironment("KernelMemory__Services__AzureOpenAIText__APIKey", s_azureOpenAITextConfig.APIKey)
            .WithEnvironment("KernelMemory__Services__AzureOpenAIText__Endpoint", s_azureOpenAITextConfig.Endpoint)
            .WithEnvironment("KernelMemory__Services__AzureOpenAIText__Deployment", s_azureOpenAITextConfig.Deployment)
            // Azure OpenAI settings - Embeddings
            .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__Auth", s_azureOpenAIEmbeddingConfig.Auth.ToString("G"))
            .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__APIKey", s_azureOpenAIEmbeddingConfig.APIKey)
            .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__Endpoint", s_azureOpenAIEmbeddingConfig.Endpoint)
            .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__Deployment", s_azureOpenAIEmbeddingConfig.Deployment)
            .PublishAsContainer();

        builder.Build().Run();
    }
}
