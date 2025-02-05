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
public sealed class DiscordMessageHandler : IPipelineStepHandler, IDisposable
{
    // Name of the file where to store Discord data
    private readonly string _filename;

    // KM pipelines orchestrator
    private readonly IPipelineOrchestrator _orchestrator;

    // .NET service provider, used to get thread safe instances of EF DbContext
    private readonly IServiceProvider _serviceProvider;

    // DB creation
    private readonly object _dbCreation = new();
    private bool _dbCreated = false;
    private bool _useScope = false;
    private readonly IServiceScope _dbScope;

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
        this._dbScope = this._serviceProvider.CreateScope();

        try
        {
            this.OnFirstInvoke();
        }
        catch (Exception)
        {
            // ignore, will retry later
        }
    }

    public async Task<(ReturnType returnType, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this.OnFirstInvoke();

        // Note: use a new DbContext instance each time, because DbContext is not thread safe and would throw the following
        // exception: System.InvalidOperationException: a second operation was started on this context instance before a previous
        // operation completed. This is usually caused by different threads concurrently using the same instance of DbContext.
        // For more information on how to avoid threading issues with DbContext, see https://go.microsoft.com/fwlink/?linkid=2097913.
        var db = this.GetDb();
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
                        return (ReturnType.FatalError, pipeline);
                    }
                }
                catch (Exception e)
                {
                    this._log.LogError(e, "Failed to deserialize Discord data file");
                    return (ReturnType.FatalError, pipeline);
                }

                await db.Messages.AddAsync(data, cancellationToken).ConfigureAwait(false);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return (ReturnType.Success, pipeline);
    }

    public void Dispose()
    {
        this._dbScope.Dispose();
    }

    private void OnFirstInvoke()
    {
        if (this._dbCreated) { return; }

        lock (this._dbCreation)
        {
            if (this._dbCreated) { return; }

            var db = this.GetDb();
            db.Database.EnsureCreated();
            db.Dispose();
            db = null;

            this._dbCreated = true;

            this._log.LogInformation("DB created");
        }
    }

    /// <summary>
    /// Depending on the hosting type, the DB Context is retrieved in different ways.
    /// Single host app:
    ///     db = _serviceProvider.GetService[DiscordDbContext](); // this throws an exception in multi-host mode
    /// Multi host app:
    ///     db = serviceProvider.CreateScope().ServiceProvider.GetRequiredService[DiscordDbContext]();
    /// </summary>
    private DiscordDbContext GetDb()
    {
        DiscordDbContext? db;

        if (this._useScope)
        {
            db = this._dbScope.ServiceProvider.GetRequiredService<DiscordDbContext>();
        }
        else
        {
            try
            {
                // Try the single app host first
                this._log.LogTrace("Retrieving Discord DB context using service provider");
                db = this._serviceProvider.GetService<DiscordDbContext>();
            }
            catch (InvalidOperationException)
            {
                // If the single app host fails, try the multi app host
                this._log.LogInformation("Retrieving Discord DB context using scope");
                db = this._dbScope.ServiceProvider.GetRequiredService<DiscordDbContext>();

                // If the multi app host succeeds, set a flag to remember to use the scope
                if (db != null)
                {
                    this._useScope = true;
                }
            }
        }

        ArgumentNullExceptionEx.ThrowIfNull(db, nameof(db), "Discord DB context is NULL");

        return db;
    }
}
