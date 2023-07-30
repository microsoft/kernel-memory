## Example: serverless, custom ingestion pipeline using \<InProcessPipelineOrchestrator>

This is a more advanced example, showing how to customize how documents
are processed and turned into memories.

The example uses `InProcessPipelineOrchestrator` to run all the code locally,
and you can see how handlers are manually defined and composed, processing
multiple files, with a fluent syntax:

```csharp
var pipeline = orchestrator
    .PrepareNewFileUploadPipeline("inProcessTest", "userZ", new TagCollection { { "testName", "example3" } })
    .AddUploadFile("file1", "file1.txt", "file1.txt")
    .AddUploadFile("file2", "file2.txt", "file2.txt")
    .AddUploadFile("file3", "file3.docx", "file3.docx")
    .AddUploadFile("file4", "file4.pdf", "file4.pdf")
    .Then("extract")
    .Then("partition")
    .Then("gen_embeddings")
    .Then("save_embeddings")
    .Build();
```

# Prepare the example

Before running the code, from the folder run this command:

```csharp
dotnet run setup
```

The app will ask a few questions about your configuration, storing the
required information in `appsettings.Development.json`.

You can run the command again later to edit the file, or edit it manually for
advanced configurations.

You can find more details about the configuration options in `appsettings.json`,
and more info about .NET configurations at
https://learn.microsoft.com/aspnet/core/fundamentals/configuration

# Run the example

To run the example:

```csharp
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

The `ASPNETCORE_ENVIRONMENT` env var is required for the code to use
the settings stored in `appsettings.Development.json`.