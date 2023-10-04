﻿// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Data.Nl2Sql;

/// <summary>
/// Entry point for console execution.
/// </summary>
/// <remarks>
/// See https://github.com/microsoft/semantic-memory/tree/main/examples/200-dotnet-nl2sql/README.md for configuration steps.
/// </remarks>
internal static class Program
{
    // ReSharper disable once InconsistentNaming
    private static async Task Main(string[] args)
    {
        using var traceListener = new TextWriterTraceListener("nl2sql.log");

        var host =
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(
                    (context, config) =>
                    {
                        config
                            .AddJsonFile("appsettings.json")
                            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                            .AddEnvironmentVariables()
                            .AddUserSecrets(Assembly.GetExecutingAssembly());
                    })
                .ConfigureLogging(
                    (context, config) =>
                    {
                        config
                            .ClearProviders()
                            .AddConfiguration(context.Configuration.GetSection("Logging"))
                            .AddTraceSource(new SourceSwitch("FileTrace", $"{SourceLevels.Verbose}"), traceListener); // Verbose to capture kernel logging
                    })
                .ConfigureServices(
                    (context, services) =>
                    {
                        services
                            .AddSingleton(KernelFactory.Create(context.Configuration))
                            .AddSingleton(SqlConnectionProvider.Create(context.Configuration))
                            .AddHostedService<Nl2SqlConsole>();
                    })
                .UseConsoleLifetime()
                .Build();

        await host.RunAsync().ConfigureAwait(false);
    }
}
