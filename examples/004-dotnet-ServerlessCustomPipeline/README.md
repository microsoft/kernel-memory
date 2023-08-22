## Example: serverless, custom ingestion pipeline using \<InProcessPipelineOrchestrator>

This is a more advanced example, showing how to customize the way documents
are processed and turned into memories.

The example uses `InProcessPipelineOrchestrator` to run all the code locally,
and you can see how handlers are manually defined and composed, processing
multiple files, with a fluent syntax:

```csharp
var pipeline = orchestrator
    .PrepareNewDocumentUpload("userZ", "inProcessTest", new TagCollection { { "type", "test" } })
    .AddUploadFile("file1", "file1.txt", "file1.txt")
    .AddUploadFile("file2", "file2.txt", "file2.txt")
    .AddUploadFile("file3", "file3.docx", "file3.docx")
    .AddUploadFile("file4", "file4.pdf", "file4.pdf")
    .Then("extract")
    .Then("partition")
    .Then("summarize")
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
required information in `appsettings.Development.json`. This file is used when
the env var `ASPNETCORE_ENVIRONMENT` is set to `Development`. Look at the
comments in `appsettings.json` for details and more advanced options.

You can run the command again later to edit the file, or edit it manually for
advanced configurations.

You can find more details about the configuration options in `appsettings.json`,
and more info about .NET configurations at
https://learn.microsoft.com/aspnet/core/fundamentals/configuration

# Run the example

To run the example, depending on your platform, execute either `run.sh` or `run.cmd`.