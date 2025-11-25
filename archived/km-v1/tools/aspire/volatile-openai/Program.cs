// Copyright (c) Microsoft. All rights reserved.

using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.Pipeline.Queue.DevTools;
using Projects;

namespace Microsoft.KernelMemory.Aspire.AppHost;

// Run KM in volatile mode, with OpenAI models
internal static class Program
{
    private static readonly SimpleFileStorageConfig s_simpleFileStorageConfig = new();
    private static readonly SimpleQueuesConfig s_simpleQueuesConfig = new();
    private static readonly SimpleVectorDbConfig s_simpleVectorDbConfig = new();
    private static readonly OpenAIConfig s_openAIConfig = new();

    internal static void Main()
    {
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:SimpleFileStorage", s_simpleFileStorageConfig)
            .BindSection("KernelMemory:Services:SimpleQueues", s_simpleQueuesConfig)
            .BindSection("KernelMemory:Services:SimpleVectorDb", s_simpleVectorDbConfig)
            .BindSection("KernelMemory:Services:OpenAI", s_openAIConfig);

        RunFromCode();
    }

    private static void RunFromCode()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddProject<Service>("kernel-memory")
            .WithHttpEndpoint(targetPort: 21001)
            .WithEnvironment("ASPNETCORE_URLS", "http://+:21001")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("KernelMemory__Service__OpenApiEnabled", "True")
            .WithKmTextEmbeddingGenerationEnvironment("OpenAI", s_openAIConfig)
            .WithKmTextGenerationEnvironment("OpenAI", s_openAIConfig)
            .WithKmMemoryDbEnvironment("SimpleVectorDb", s_simpleVectorDbConfig)
            .WithKmOrchestrationEnvironment("SimpleQueues", s_simpleQueuesConfig)
            .WithKmDocumentStorageEnvironment("SimpleFileStorage", s_simpleFileStorageConfig)
            .WithKmContentSafetyModerationEnvironment(null) // ensure moderation is disabled
            .WithKmOcrEnvironment(null); // ensure OCR is disabled;

        builder
            .ShowDashboardUrl()
            .LaunchDashboard()
            .Build().Run();
    }
}
