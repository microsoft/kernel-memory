// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.Sources.DiscordBot;

namespace Microsoft.Discord.TestApplication;

/* Example: Listen for new messages in Discord, and save them in a table in Postgres.
 *
 * Why this example: You can build on this example to populate a memory database with
 *                   user messages, and then use the memory database to autogenerate answers.
 *
 * Use ASP.NET hosted services to host a Discord Bot. The discord bot logic is based
 * on DiscordConnector class.
 *
 * While the Discord bot is running, every time there is a new message, DiscordConnector
 * invokes KM.ImportDocument API, uploading a JSON file that contains details about the
 * Discord message, including server ID, channel ID, author ID, message content, etc.
 *
 * The call to KM.ImportDocument API asks to process the JSON file uploaded using
 * DiscordMessageHandler, included in this project. No other handlers are used.
 *
 * DiscordMessageHandler, loads the JSON file uploaded, deserializes its content, and
 * saves each Discord message into a table in Postgres, using Entity Framework.
 *
 * Discord Server
 *   => Discord Bot
 *      => OnMessage Event
 *         => KM.ImportDocumentAsync(data, steps: ["store_discord_message"])
 *            => DiscordMessageHandler
 *               => Postgres table
 */

internal static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();

        appBuilder.Configuration
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        // Discord setup
        // Use DiscordConnector to connect to Discord and listen for messages.
        // The Discord connection can listen from multiple servers and channels.
        // For each message, DiscordConnector will send a file to Kernel Memory to process.
        // Files sent to Kernel Memory are processed by DiscordMessageHandler (in this project)
        var discordCfg = appBuilder.Configuration.GetSection("Discord").Get<DiscordConnectorConfig>();
        ArgumentNullExceptionEx.ThrowIfNull(discordCfg, nameof(discordCfg), "Discord config is NULL");
        appBuilder.Services.AddSingleton<DiscordConnectorConfig>(discordCfg);
        appBuilder.Services.AddHostedService<DiscordConnector>();

        // Postgres with Entity Framework
        // DiscordMessageHandler reads files received by Kernel Memory and store each message in a table in Postgres.
        // See DiscordDbMessage for the table schema.
        appBuilder.AddNpgsqlDbContext<DiscordDbContext>("postgresDb");

        // Run Kernel Memory and DiscordMessageHandler
        // var kmApp = BuildAsynchronousKernelMemoryApp(appBuilder, discordCfg); // run using queues and threads
        var kmApp = BuildSynchronousKernelMemoryApp(appBuilder, discordCfg); // run everything in one thread

        Console.WriteLine("Starting KM application...\n");
        kmApp.Run();
        Console.WriteLine("\n... KM application stopped.");
    }

    private static WebApplication BuildSynchronousKernelMemoryApp(WebApplicationBuilder appBuilder, DiscordConnectorConfig discordConfig)
    {
        appBuilder.AddKernelMemory(kmb =>
        {
            // Note: there's no queue system, so the memory instance will be synchronous (ie MemoryServerless)

            // Store files and vectors on disk
            kmb.WithSimpleFileStorage(SimpleFileStorageConfig.Persistent);
            kmb.WithSimpleVectorDb(SimpleVectorDbConfig.Persistent);

            // Disable AI, not needed for this example
            kmb.WithoutEmbeddingGenerator();
            kmb.WithoutTextGenerator();
        });

        WebApplication app = appBuilder.Build();

        // In synchronous apps, handlers are added to the serverless memory orchestrator
        var orchestrator = (app.Services.GetService<IKernelMemory>() as MemoryServerless)!.Orchestrator;
        orchestrator.AddHandler<DiscordMessageHandler>(discordConfig.Steps[0]);

        return app;
    }

    private static WebApplication BuildAsynchronousKernelMemoryApp(WebApplicationBuilder appBuilder, DiscordConnectorConfig discordConfig)
    {
        appBuilder.AddKernelMemory(kmb =>
        {
            // Note: because of this the memory instance will be asynchronous (ie MemoryService)
            kmb.WithSimpleQueuesPipeline();

            // Store files and vectors on disk
            kmb.WithSimpleFileStorage(SimpleFileStorageConfig.Persistent);
            kmb.WithSimpleVectorDb(SimpleVectorDbConfig.Persistent);

            // Disable AI, not needed for this example
            kmb.WithoutEmbeddingGenerator();
            kmb.WithoutTextGenerator();
        });

        // In asynchronous apps, handlers are added as hosted services to run on dedicated threads
        appBuilder.Services.AddHandlerAsHostedService<DiscordMessageHandler>(discordConfig.Steps[0]);

        WebApplication app = appBuilder.Build();

        return app;
    }
}
