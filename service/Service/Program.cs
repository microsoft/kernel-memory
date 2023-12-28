// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory.InteractiveSetup;
using Microsoft.KernelMemory.Service.Core;

// ********************************************************
// ************** APP SETTINGS ****************************
// ********************************************************

// Run `dotnet run setup` to run this code and setup the service
if (new[] { "setup", "-setup" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
{
    Main.InteractiveSetup(cfgService: true);
}

// ********************************************************
// ************** APP BUILD *******************************
// ********************************************************

// Usual .NET web app builder
var appBuilder = WebApplication.CreateBuilder();

appBuilder.AddKernelMemoryWithDefaults();

// Build .NET web app as usual
var app = appBuilder.Build();

//Console.WriteLine("***************************************************************************************************************************");
//Console.WriteLine($"* Web service         : " + (config.Service.RunWebService ? "Enabled" : "Disabled"));
//Console.WriteLine($"* Web service auth    : " + (config.ServiceAuthorization.Enabled ? "Enabled" : "Disabled"));
//Console.WriteLine($"* Pipeline handlers   : " + (config.Service.RunHandlers ? "Enabled" : "Disabled"));
//Console.WriteLine($"* OpenAPI swagger     : " + (config.Service.OpenApiEnabled ? "Enabled" : "Disabled"));
//Console.WriteLine($"* Logging level       : {app.Logger.GetLogLevelName()}");
//Console.WriteLine($"* Memory Db           : {app.Services.GetService<IMemoryDb>()?.GetType().FullName}");
//Console.WriteLine($"* Content storage     : {app.Services.GetService<IContentStorage>()?.GetType().FullName}");
//Console.WriteLine($"* Embedding generation: {app.Services.GetService<ITextEmbeddingGenerator>()?.GetType().FullName}");
//Console.WriteLine($"* Text generation     : {app.Services.GetService<ITextGenerator>()?.GetType().FullName}");
//Console.WriteLine("***************************************************************************************************************************");

// ********************************************************
// ************** WEB SERVICE ENDPOINTS *******************
// ********************************************************

// ReSharper disable once TemplateIsNotCompileTimeConstantProblem

DateTimeOffset start = DateTimeOffset.UtcNow;
// Simple ping endpoint
app.MapGet("/", () => Results.Ok("Ingestion service is running. " +
                                 "Uptime: " + (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                               - start.ToUnixTimeSeconds()) + " secs " +
                                 $"- Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}"))
    .Produces<string>(StatusCodes.Status200OK)
    .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
    .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

app.AddKernelMemoryEndpoints();

// ********************************************************
// ************** START ***********************************
// ********************************************************

app.Run();
