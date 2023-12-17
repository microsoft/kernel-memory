// Copyright (c) Microsoft. All rights reserved.

using Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory;
using Microsoft.OpenApi.Models;
using Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors((options) =>
{
    options.AddDefaultPolicy((policy) =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOptions<KernelMemoryOptions>().Bind(builder.Configuration.GetSection(KernelMemoryOptions.KernelMemory));

builder.Services.AddHealthChecks();

builder.Services.AddAuthorization();

builder.Services.AddKernelMemory();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen((options) =>
{
    options.SwaggerDoc("v1", new OpenApiInfo()
    {
        Title = "Kernel Memory Document Ingestion & Query",
        Version = "v1",
        Description = "Integration for leveraging Kernel Memory to ingest documents and query them",
    });
});

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)));
});

var app = builder.Build();

app.UseCors();

app.UseSwagger();

app.UseSwaggerUI();

app.UseOutputCache();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapPost("/ingestion", async ([FromServices] MemoryServerless memory, IFormFileCollection files, string documentName) =>
{
    Document document = new(documentName);
    foreach (var file in files)
    {
        document.AddStream(file.FileName, file.OpenReadStream());
    }
    await memory.ImportDocumentAsync(document, steps: new[] { Constants.PipelineStepsDeleteGeneratedFiles });
    return Results.Ok();
})
.DisableAntiforgery()
.WithName("Document Ingestion")
.WithDescription("Ingests documents")
.WithTags("Document Ingestion and Retrieval");

app.MapGet("/retrieval", async ([FromServices] MemoryServerless memory, ILogger<Program> logger, string query) =>
{
    logger.LogInformation("Query : {0}", query);
    var result = await memory.AskAsync(query);
    if (result.NoResult)
    {
        return Results.NotFound(result.Result);
    }
    else
    {
        return Results.Ok(result.Result);
    }
})
.CacheOutput()
.WithName("Document Retrieval")
.WithTags("Document Ingestion and Retrieval")
.WithOpenApi((operation) =>
{
    operation.Description = "Retrieves data from an ingested document based on user query";
    var parameter = operation.Parameters[0];
    parameter.Name = "Query";
    return operation;
});

app.Run();
