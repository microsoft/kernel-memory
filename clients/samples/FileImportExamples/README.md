# Setup

Before running the code, you will need some configuration step.

1. Copy `appsettings.json` to `appsettings.Development.json`
   (you could edit the original file, just be careful not sending the edited
   file to git/pull requests because it will contain personal settings and
   potential secret credentials.)
2. Edit `appsettings.Development.json` and choose one embedding generator,
   uncommenting the corresponding like (either `AzureAda` or `OpenAIAda`).
3. If you choose `AzureAda` specify your Azure OpenAI deployment name and
   Azure OpenAI API Key in the relevant block.
4. If you choose `OpenAIAda` specify your OpenAI deployment OpenAI API Key
   in the relevant block.

Set up your .NET env to dev mode, setting the env var:

    ASPNETCORE_ENVIRONMENT=Development

This will tell the code to load settings also from `appsettings.Development.json`.

For more options and advanced configurations see https://learn.microsoft.com/aspnet/core/fundamentals/configuration

# Examples

Open `Program.cs` and choose which examples to run, setting `examplesToRun`.
By default, the all examples will be executed in series.

## Example 1: serverless, no deployment, using \<MemoryPipelineClient>

This example shows how to import multiple files and ask a question, without
deploying the Semantic Memory services.

All the logic is executed locally using the default C# handlers. Depending
on your settings, files can be stored locally or in Azure Blobs.

```csharp
var memory = var memory = new MemoryPipelineClient();

await memory.ImportFilesAsync(new[] { "file2.txt", "file3.docx", "file4.pdf" },
            new ImportFileOptions("example2-user", "collection01"));

string answer = await memory.AskAsync("What's SK?");
```

## Example 2: using the service with \<MemoryWebClient>

This example shows how to import multiple files and ask a question, delegating
the work to the Semantic Memory services.

The example points to `https://127.0.0.1:9001` so you should start the services
locally before running this example:

1. Start `server/webservice-dotnet/WebService.csproj`
   * Note: you'll need to configure the service, see
     `server/webservice-dotnet/appsettings.json` for details
   * This web service will be used to upload files, to start the
     file processing pipeline, and to provide answers.
2. Start `server/pipelineservice-dotnet/PipelineService.csproj`
   * Note: you'll need to configure the service, see
     `server/pipelineservice-dotnet/appsettings.json` for details
   * This service will be used to process the files uploaded,
     using multiple _handlers_ to extract text, embeddings, etc.

```csharp
var memory = new MemoryWebClient("http://127.0.0.1:9001/");

await memory.ImportFilesAsync(new[] { "file2.txt", "file3.docx", "file4.pdf" },
            new ImportFileOptions("example2-user", "collection01"));

string answer = await memory.AskAsync("What's SK?");
```

## Example 3: serverless, custom ingestion pipeline using \<InProcessPipelineOrchestrator>

This is a more advanced example, showing how to customize how data is
processed and turned into memories.

The example uses `InProcessPipelineOrchestrator` to run all the code locally,
and you can see how handlers are manually defined and composed, processing
multiple files, with a fluent syntax:

```csharp
var pipeline = orchestrator
    .PrepareNewFileUploadPipeline("inProcessTest", "userId", new[] { "collection1" })
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