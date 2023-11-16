// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Xunit.Abstractions;

namespace FunctionalTests.TestHelpers;

public abstract class BaseTestCase : IDisposable
{
    private readonly IConfiguration _cfg;
    private readonly RedirectConsole _output;

    protected IConfiguration Configuration => this._cfg;
    protected IConfiguration ServiceConfiguration => this.Configuration.GetSection("Services");
    protected IConfiguration OpenAIConfiguration => this.ServiceConfiguration.GetSection("OpenAI");
    protected IConfiguration QdrantConfiguration => this.ServiceConfiguration.GetSection("Qdrant");
    protected IConfiguration AzureAISearchConfiguration => this.ServiceConfiguration.GetSection("AzureCognitiveSearch");

    protected BaseTestCase(IConfiguration cfg, ITestOutputHelper output)
    {
        this._cfg = cfg;
        this._output = new RedirectConsole(output);
        Console.SetOut(this._output);
    }

    protected IKernelMemory GetMemory(string memoryType)
    {
        var openAIKey = Env.Var("OPENAI_API_KEY");

        switch (memoryType)
        {
            case "default":
                return new KernelMemoryBuilder()
                    .WithOpenAIDefaults(openAIKey)
                    .BuildServerlessClient();

            case "simple_on_disk":
                return new KernelMemoryBuilder()
                    .WithOpenAIDefaults(openAIKey)
                    .WithSimpleVectorDb(new SimpleVectorDbConfig { Directory = "_vectors", StorageType = FileSystemTypes.Disk })
                    .WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = "_files", StorageType = FileSystemTypes.Disk })
                    .BuildServerlessClient();

            case "simple_volatile":
                return new KernelMemoryBuilder()
                    .WithOpenAIDefaults(openAIKey)
                    .WithSimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Volatile })
                    .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile })
                    .BuildServerlessClient();

            case "qdrant":
                var qdrantEndpoint = this.QdrantConfiguration.GetValue<string>("Endpoint");
                Assert.False(string.IsNullOrEmpty(qdrantEndpoint));
                return new KernelMemoryBuilder()
                    .WithOpenAIDefaults(openAIKey)
                    .WithQdrant(qdrantEndpoint)
                    .BuildServerlessClient();

            case "acs":
                var acsEndpoint = this.AzureAISearchConfiguration.GetValue<string>("Endpoint");
                var acsKey = this.AzureAISearchConfiguration.GetValue<string>("APIKey");
                Assert.False(string.IsNullOrEmpty(acsEndpoint));
                Assert.False(string.IsNullOrEmpty(acsKey));
                return new KernelMemoryBuilder()
                    .WithOpenAIDefaults(openAIKey)
                    .WithAzureCognitiveSearch(acsEndpoint, acsKey)
                    .BuildServerlessClient();

            default:
                throw new ArgumentOutOfRangeException($"{memoryType} not supported");
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Log(string text)
    {
        this._output.WriteLine(text);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._output.Dispose();
        }
    }
}
