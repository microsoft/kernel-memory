// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.Sources.DiscordBot;

namespace Microsoft.Discord.TestApplication;

internal static class Program
{
    public static void Main()
    {
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();

        var appSettings = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var discordConfig = new DiscordConnectorConfig();
        appSettings.GetSection("Discord").Bind(discordConfig);
        appBuilder.Services.AddSingleton(discordConfig);
        appBuilder.Services.AddHostedService<DiscordConnector>();

        // var app = BuildAsynchronousApp(appBuilder, discordConfig);
        var app = BuildSynchronousApp(appBuilder, discordConfig);

        Console.WriteLine("Starting application...");
        app.Run();
        Console.WriteLine("... application stopped.");
    }

    private static WebApplication BuildAsynchronousApp(WebApplicationBuilder appBuilder, DiscordConnectorConfig discordConfig)
    {
        appBuilder.Services.AddHandlerAsHostedService<DiscordMessageHandler>(discordConfig.Steps[0]);
        appBuilder.AddKernelMemory(kmb =>
        {
            // Note: because of this the memory instance will be asynchronous (ie MemoryService)
            kmb.WithSimpleQueuesPipeline();

            // Store files on disk
            kmb.WithSimpleFileStorage(SimpleFileStorageConfig.Persistent);

            // Required internally, won't be used
            kmb.WithOpenAIDefaults("no key");
        });

        return appBuilder.Build();
    }

    private static WebApplication BuildSynchronousApp(WebApplicationBuilder appBuilder, DiscordConnectorConfig discordConfig)
    {
        appBuilder.AddKernelMemory(kmb =>
        {
            // Note: there's no queue system, so the memory instance will be synchronous (ie MemoryServerless)

            // Store files on disk
            kmb.WithSimpleFileStorage(SimpleFileStorageConfig.Persistent);

            // Required internally, won't be used
            kmb.WithOpenAIDefaults("no key");
        });

        WebApplication app = appBuilder.Build();

        // In synchronous apps, handlers are added to the serverless memory orchestrator
        (app.Services.GetService<IKernelMemory>() as MemoryServerless)!
            .Orchestrator
            .AddHandler<DiscordMessageHandler>(discordConfig.Steps[0]);

        return app;
    }
}
