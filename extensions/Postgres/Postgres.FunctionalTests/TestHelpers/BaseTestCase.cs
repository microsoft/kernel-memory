// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

namespace Postgres.FunctionalTests.TestHelpers;

public abstract class BaseTestCase : IDisposable
{
    private readonly RedirectConsole _output;

    protected IConfiguration Configuration { get; }
    protected IConfiguration ServiceConfiguration => this.Configuration.GetSection("Services");
    protected PostgresConfig PostgresConfiguration => this.GetServiceConfig<PostgresConfig>("Postgres");
    protected OpenAIConfig OpenAIConfiguration => this.GetServiceConfig<OpenAIConfig>("OpenAI");
    protected AzureOpenAIConfig AzureOpenAITextConfiguration => this.GetServiceConfig<AzureOpenAIConfig>("AzureOpenAIText");
    protected AzureOpenAIConfig AzureOpenAIEmbeddingConfiguration => this.GetServiceConfig<AzureOpenAIConfig>("AzureOpenAIEmbedding");

    // IMPORTANT: install Xunit.DependencyInjection package
    protected BaseTestCase(IConfiguration cfg, ITestOutputHelper output)
    {
        this.Configuration = cfg;
        this._output = new RedirectConsole(output);
        Console.SetOut(this._output);
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

    private T GetServiceConfig<T>(string name)
    {
        return this.ServiceConfiguration.GetSection(name).Get<T>()
               ?? throw new ArgumentNullException(
                   $"{name} configuration not found. Check for a 'Services:{name}' " +
                   "section in appsettings.json and appsettings.development.json");
    }
}
