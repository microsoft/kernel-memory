// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.Postgres;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Xunit.Abstractions;

namespace FunctionalTests3P.TestHelpers;

public abstract class BaseTestCase : IDisposable
{
    protected const string NotFound = "INFO NOT FOUND";

    private readonly IConfiguration _cfg;
    private readonly RedirectConsole _output;

    protected IConfiguration Configuration => this._cfg;
    protected IConfiguration ServiceConfiguration => this.Configuration.GetSection("Services");
    protected IConfiguration OpenAIConfiguration => this.ServiceConfiguration.GetSection("OpenAI");
    protected IConfiguration QdrantConfiguration => this.ServiceConfiguration.GetSection("Qdrant");
    protected IConfiguration AzureAISearchConfiguration => this.ServiceConfiguration.GetSection("AzureAISearch");
    protected IConfiguration PostgresConfiguration => this.ServiceConfiguration.GetSection("Postgres");

    // IMPORTANT: install Xunit.DependencyInjection package
    protected BaseTestCase(IConfiguration cfg, ITestOutputHelper output)
    {
        this._cfg = cfg;
        this._output = new RedirectConsole(output);
        Console.SetOut(this._output);
    }

    protected IKernelMemory GetServerlessMemory(string memoryType)
    {
        var openAIKey = this.OpenAIConfiguration.GetValue<string>("APIKey")
                        ?? throw new TestCanceledException("OpenAI API key is missing");

        switch (memoryType)
        {
            case "default":
                return new KernelMemoryBuilder()
                    .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
                    .WithOpenAIDefaults(openAIKey)
                    .Build<MemoryServerless>();

            case "simple_on_disk":
                return new KernelMemoryBuilder()
                    .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
                    .WithOpenAIDefaults(openAIKey)
                    .WithSimpleVectorDb(new SimpleVectorDbConfig { Directory = "_vectors", StorageType = FileSystemTypes.Disk })
                    .WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = "_files", StorageType = FileSystemTypes.Disk })
                    .Build<MemoryServerless>();

            case "simple_volatile":
                return new KernelMemoryBuilder()
                    .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
                    .WithOpenAIDefaults(openAIKey)
                    .WithSimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Volatile })
                    .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile })
                    .Build<MemoryServerless>();

            case "qdrant":
                var qdrantEndpoint = this.QdrantConfiguration.GetValue<string>("Endpoint");
                Assert.False(string.IsNullOrEmpty(qdrantEndpoint));
                return new KernelMemoryBuilder()
                    .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
                    .WithOpenAIDefaults(openAIKey)
                    .WithQdrant(qdrantEndpoint)
                    .Build<MemoryServerless>();

            case "az_ai_search":
                var acsEndpoint = this.AzureAISearchConfiguration.GetValue<string>("Endpoint");
                var acsKey = this.AzureAISearchConfiguration.GetValue<string>("APIKey");
                Assert.False(string.IsNullOrEmpty(acsEndpoint));
                Assert.False(string.IsNullOrEmpty(acsKey));
                return new KernelMemoryBuilder()
                    .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
                    .WithOpenAIDefaults(openAIKey)
                    .WithAzureAISearch(acsEndpoint, acsKey)
                    .Build<MemoryServerless>();

            case "postgres":
                var connString = this.PostgresConfiguration.GetValue<string>("ConnectionString");
                Assert.False(string.IsNullOrEmpty(connString));
                return new KernelMemoryBuilder()
                    .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
                    .WithOpenAIDefaults(openAIKey)
                    .WithPostgres(connString)
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
