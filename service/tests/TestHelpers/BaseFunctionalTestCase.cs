// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.MongoDbAtlas;

namespace Microsoft.KM.TestHelpers;

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
    protected readonly MongoDbAtlasConfig MongoDbAtlasConfig;
    protected readonly SimpleVectorDbConfig SimpleVectorDbConfig;
    protected readonly LlamaSharpConfig LlamaSharpConfig;
    protected readonly ElasticsearchConfig ElasticsearchConfig;
    protected readonly OnnxConfig OnnxConfig;

    // IMPORTANT: install Xunit.DependencyInjection package
    protected BaseFunctionalTestCase(IConfiguration cfg, ITestOutputHelper output)
    {
        this._cfg = cfg;
        this._output = new RedirectConsole(output);
        Console.SetOut(this._output);

        this.OpenAiConfig = cfg.GetSection("KernelMemory:Services:OpenAI").Get<OpenAIConfig>() ?? new();
        this.AzureOpenAITextConfiguration = cfg.GetSection("KernelMemory:Services:AzureOpenAIText").Get<AzureOpenAIConfig>() ?? new();
        this.AzureOpenAIEmbeddingConfiguration = cfg.GetSection("KernelMemory:Services:AzureOpenAIEmbedding").Get<AzureOpenAIConfig>() ?? new();
        this.AzureAiSearchConfig = cfg.GetSection("KernelMemory:Services:AzureAISearch").Get<AzureAISearchConfig>() ?? new();
        this.QdrantConfig = cfg.GetSection("KernelMemory:Services:Qdrant").Get<QdrantConfig>() ?? new();
        this.PostgresConfig = cfg.GetSection("KernelMemory:Services:Postgres").Get<PostgresConfig>() ?? new();
        this.RedisConfig = cfg.GetSection("KernelMemory:Services:Redis").Get<RedisConfig>() ?? new();
        this.MongoDbAtlasConfig = cfg.GetSection("KernelMemory:Services:MongoDbAtlas").Get<MongoDbAtlasConfig>() ?? new();
        this.SimpleVectorDbConfig = cfg.GetSection("KernelMemory:Services:SimpleVectorDb").Get<SimpleVectorDbConfig>() ?? new();
        this.LlamaSharpConfig = cfg.GetSection("KernelMemory:Services:LlamaSharp").Get<LlamaSharpConfig>() ?? new();
        this.ElasticsearchConfig = cfg.GetSection("KernelMemory:Services:Elasticsearch").Get<ElasticsearchConfig>() ?? new();
        this.OnnxConfig = cfg.GetSection("KernelMemory:Services:Onnx").Get<OnnxConfig>() ?? new();
    }

    protected IKernelMemory GetMemoryWebClient()
    {
        string endpoint = this._cfg.GetSection("KernelMemory:ServiceAuthorization").GetValue<string>("Endpoint", "http://127.0.0.1:9001/")!;
        string? apiKey = this._cfg.GetSection("KernelMemory:ServiceAuthorization").GetValue<string>("AccessKey");
        return new MemoryWebClient(endpoint, apiKey: apiKey);
    }

    protected IKernelMemory GetServerlessMemory(string memoryType)
    {
        var builder = new KernelMemoryBuilder()
            .Configure(kmb => kmb.Services.AddLogging(b => { b.AddConsole().SetMinimumLevel(LogLevel.Trace); }))
            .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
            .WithOpenAI(this.OpenAiConfig);

        switch (memoryType)
        {
            case "default":
                return builder.Build<MemoryServerless>();

            case "simple_on_disk":
                return builder
                    .WithSimpleVectorDb(new SimpleVectorDbConfig { Directory = "_vectors", StorageType = FileSystemTypes.Disk })
                    .WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = "_files", StorageType = FileSystemTypes.Disk })
                    .Build<MemoryServerless>();

            case "simple_volatile":
                return builder
                    .WithSimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Volatile })
                    .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile })
                    .Build<MemoryServerless>();

            default:
                throw new ArgumentOutOfRangeException(nameof(memoryType), $"{memoryType} not supported");
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
