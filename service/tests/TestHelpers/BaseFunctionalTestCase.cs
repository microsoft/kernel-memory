// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Xunit.Abstractions;

namespace Microsoft.TestHelpers;

public abstract class BaseFunctionalTestCase : IDisposable
{
    protected const string NotFound = "INFO NOT FOUND";

    private readonly IConfiguration _cfg;
    private readonly RedirectConsole _output;

    protected readonly OpenAIConfig OpenAiConfig;
    protected readonly AzureOpenAIConfig AzureOpenAITextConfiguration;
    protected readonly AzureOpenAIConfig AzureOpenAIEmbeddingConfiguration;
    protected readonly AzureAISearchConfig AzureAiSearchConfig;
    protected readonly QdrantConfig QdrantConfig;
    protected readonly PostgresConfig PostgresConfig;
    protected readonly RedisConfig RedisConfig;
    protected readonly SimpleVectorDbConfig SimpleVectorDbConfig;
    protected readonly LlamaSharpConfig LlamaSharpConfig;

    // IMPORTANT: install Xunit.DependencyInjection package
    protected BaseFunctionalTestCase(IConfiguration cfg, ITestOutputHelper output)
    {
        this._cfg = cfg;
        this._output = new RedirectConsole(output);
        Console.SetOut(this._output);

        this.OpenAiConfig = cfg.GetSection("Services:OpenAI").Get<OpenAIConfig>()!;
        this.AzureOpenAITextConfiguration = cfg.GetSection("Services:AzureOpenAIText").Get<AzureOpenAIConfig>()!;
        this.AzureOpenAIEmbeddingConfiguration = cfg.GetSection("Services:AzureOpenAIEmbedding").Get<AzureOpenAIConfig>()!;
        this.AzureAiSearchConfig = cfg.GetSection("Services:AzureAISearch").Get<AzureAISearchConfig>()!;
        this.QdrantConfig = cfg.GetSection("Services:Qdrant").Get<QdrantConfig>()!;
        this.PostgresConfig = cfg.GetSection("Services:Postgres").Get<PostgresConfig>()!;
        this.RedisConfig = cfg.GetSection("Services:Redis").Get<RedisConfig>()!;
        this.SimpleVectorDbConfig = cfg.GetSection("Services:SimpleVectorDb").Get<SimpleVectorDbConfig>()!;
        this.LlamaSharpConfig = cfg.GetSection("Services:LlamaSharp").Get<LlamaSharpConfig>()!;
    }

    protected IKernelMemory GetMemoryWebClient()
    {
        string endpoint = this._cfg.GetSection("ServiceAuthorization").GetValue<string>("Endpoint", "http://127.0.0.1:9001/")!;
        string? apiKey = this._cfg.GetSection("ServiceAuthorization").GetValue<string>("AccessKey");
        return new MemoryWebClient(endpoint, apiKey: apiKey);
    }

    protected IKernelMemory GetServerlessMemory(string memoryType)
    {
        switch (memoryType)
        {
            case "default":
                return new KernelMemoryBuilder()
                    .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
                    .WithOpenAI(this.OpenAiConfig)
                    .Build<MemoryServerless>();

            case "simple_on_disk":
                return new KernelMemoryBuilder()
                    .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
                    .WithOpenAI(this.OpenAiConfig)
                    .WithSimpleVectorDb(new SimpleVectorDbConfig { Directory = "_vectors", StorageType = FileSystemTypes.Disk })
                    .WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = "_files", StorageType = FileSystemTypes.Disk })
                    .Build<MemoryServerless>();

            case "simple_volatile":
                return new KernelMemoryBuilder()
                    .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
                    .WithOpenAI(this.OpenAiConfig)
                    .WithSimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Volatile })
                    .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile })
                    .Build<MemoryServerless>();

            default:
                throw new ArgumentOutOfRangeException($"{memoryType} not supported");
        }
    }

    // Find the "Fixtures" directory (inside the project, requires source code)
    protected string? FindFixturesDir()
    {
        // start from the location of the executing assembly, and traverse up max 5 levels
        var path = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));
        for (var i = 0; i < 5; i++)
        {
            Console.WriteLine($"Checking '{path}'");
            var test = Path.Join(path, "Fixtures");
            if (Directory.Exists(test)) { return test; }

            // up one level
            path = Path.GetDirectoryName(path);
        }

        return null;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._output.Dispose();
        }
    }

    protected void Log(string text)
    {
        this._output.WriteLine(text);
    }
}
