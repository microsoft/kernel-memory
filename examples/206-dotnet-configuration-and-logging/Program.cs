// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

public static class Program
{
    // ReSharper disable once InconsistentNaming
    public static async Task Main()
    {
        var openAIConfig = new OpenAIConfig
        {
            TextModel = "gpt-3.5-turbo-16k",
            EmbeddingModel = "text-embedding-ada-002",
            EmbeddingModelMaxTokenTotal = 8191,
            APIKey = "sk-..."
        };

        // // If you want to load settings from some data source like config files, env vars, Azure Configuration Service, etc.
        // // These data sources can include Logging settings.
        // new ConfigurationBuilder()
        //     // Read settings from files
        //     .AddJsonFile("appsettings.json")
        //     .AddJsonFile("appsettings.Development.json", optional: true)
        //     .AddIniFile("...")
        //     .AddXmlFile("...")
        //     // Read settings from env vars. Env var names follow a specific convention, replacing ":" with "__", e.g. "Logging__LogLevel__Default"
        //     .AddEnvironmentVariables()
        //     // Read settings from .NET secret manager, see https://learn.microsoft.com/aspnet/core/security/app-secrets
        //     .AddUserSecrets(Assembly.GetEntryAssembly())
        //     // Read settings from Azure App Configuration, see https://learn.microsoft.com/azure/azure-app-configuration/overview
        //     .AddAzureAppConfiguration()
        //     // Read from Azure KeyVault, see https://learn.microsoft.com/aspnet/core/security/key-vault-configuration
        //     .AddAzureKeyVault()
        //     // Merge all data sources, in order
        //     .Build()
        //     // Fill object with data from a specific configuration section
        //     .BindSection("KernelMemory:Services:OpenAI", openAIConfig);

        var kernelMemoryBuilder = new KernelMemoryBuilder()
            .WithOpenAI(openAIConfig);

        kernelMemoryBuilder.Services
            .AddLogging(c =>
            {
                c.AddConsole().SetMinimumLevel(LogLevel.Warning); // <== Log Level
            });

        var memory = kernelMemoryBuilder.Build();

        Console.WriteLine("# START");

        // Run some code and observe the console for log entries. With LogLevel.Warning the console should be empty.
        // Change the log level to LogLevel.Information / LogLevel.Debug / LogLevel.Trace to see more log entries.
        await memory.ImportTextAsync("In physics, mass–energy equivalence is the relationship between mass and energy " +
                                     "in a system's rest frame, where the two quantities differ only by a multiplicative " +
                                     "constant and the units of measurement. The principle is described by the physicist " +
                                     "Albert Einstein's formula: E = m*c^2", documentId: "test");

        Console.WriteLine("# END");
    }
}
