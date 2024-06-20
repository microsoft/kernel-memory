// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Sources.DiscordBot;

namespace Microsoft.Discord.TestApplication;

/// <summary>
/// KM pipeline handler fetching discord data files from document storage
/// and storing messages in Postgres.
/// </summary>
public sealed class DiscordMessageHandler : IPipelineStepHandler, IDisposable, IAsyncDisposable
{
    // Name of the file where to store Discord data
    private readonly string _filename;

    // KM pipelines orchestrator
    private readonly IPipelineOrchestrator _orchestrator;

    // .NET service provider, used to get thread safe instances of EF DbContext
    private readonly IServiceProvider _serviceProvider;

    // EF DbContext used to create the database
    private DiscordDbContext? _firstInvokeDb;

    // .NET logger
    private readonly ILogger<DiscordMessageHandler> _log;

    public string StepName { get; } = string.Empty;

    public DiscordMessageHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        DiscordConnectorConfig config,
        IServiceProvider serviceProvider,
        ILoggerFactory? loggerFactory = null)
    {
        this.StepName = stepName;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<DiscordMessageHandler>();

        this._orchestrator = orchestrator;
        this._serviceProvider = serviceProvider;
        this._filename = config.FileName;

        // This DbContext instance is used only to create the database
        this._firstInvokeDb = serviceProvider.GetService<DiscordDbContext>() ?? throw new ConfigurationException("Discord DB Content is not defined");
    }

    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this.OnFirstInvoke();

        // Note: use a new DbContext instance each time, because DbContext is not thread safe and would throw the following
        // exception: System.InvalidOperationException: a second operation was started on this context instance before a previous
        // operation completed. This is usually caused by different threads concurrently using the same instance of DbContext.
        // For more information on how to avoid threading issues with DbContext, see https://go.microsoft.com/fwlink/?linkid=2097913.
        DiscordDbContext? db = this._serviceProvider.GetService<DiscordDbContext>();
        ArgumentNullExceptionEx.ThrowIfNull(db, nameof(db), "Discord DB context is NULL");
        await using (db.ConfigureAwait(false))
        {
            foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
            {
                // Process only the file containing the discord data
                if (uploadedFile.Name != this._filename) { continue; }

                string fileContent = await this._orchestrator.ReadTextFileAsync(pipeline, uploadedFile.Name, cancellationToken).ConfigureAwait(false);

                DiscordDbMessage? data;
                try
                {
                    data = JsonSerializer.Deserialize<DiscordDbMessage>(fileContent);
                    if (data == null)
                    {
                        this._log.LogError("Failed to deserialize Discord data file, result is NULL");
                        return (true, pipeline);
                    }
                }
                catch (Exception e)
                {
                    this._log.LogError(e, "Failed to deserialize Discord data file");
                    return (true, pipeline);
                }

                await db.Messages.AddAsync(data, cancellationToken).ConfigureAwait(false);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return (true, pipeline);
    }

    public void Dispose()
    {
        this._firstInvokeDb?.Dispose();
        this._firstInvokeDb = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (this._firstInvokeDb != null) { await this._firstInvokeDb.DisposeAsync(); }

        this._firstInvokeDb = null;
    }

    private void OnFirstInvoke()
    {
        if (this._firstInvokeDb == null) { return; }

        lock (this._firstInvokeDb)
        {
            // Create DB / Tables if needed
            this._firstInvokeDb.Database.EnsureCreated();
            this._firstInvokeDb.Dispose();
            this._firstInvokeDb = null;
        }
    }
}
