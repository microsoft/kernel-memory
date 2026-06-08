## Example: async memory, custom ingestion pipeline

This is a more advanced example, showing how to customize the way documents
are processed and turned into memories when using asynchronous memory pipelines.

The example uses `AddHandlerAsHostedService` to set which handlers to run as services
in the hosting application:

```csharp
host.Services.AddHandlerAsHostedService<TextExtractionHandler>("extract_text");
host.Services.AddHandlerAsHostedService<TextPartitioningHandler>("split_text_in_partitions");
host.Services.AddHandlerAsHostedService<SummarizationHandler>("summarize");
host.Services.AddHandlerAsHostedService<GenerateEmbeddingsHandler>("generate_embeddings");
host.Services.AddHandlerAsHostedService<SaveRecordsHandler>("save_memory_records");
```

and the `steps` parameter to choose which handlers to use
during the ingestion:

```csharp
string docId = await memory.ImportDocumentAsync(
    new Document("inProcessTest")
        .AddFile("file1-Wikipedia-Carbon.txt")
        .AddFile("file2-Wikipedia-Moon.txt")
        .AddFile("file3-lorem-ipsum.docx")
        .AddFile("file4-KM-Readme.pdf")
        .AddFile("file5-NASA-news.pdf")
        .AddTag("testName", "example3"),
    steps: new[]
    {
        "extract_text",
        "split_text_in_partitions",
        "generate_embeddings",
        "save_memory_records"
    });
```

Please see https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services for more
information about .NET background tasks.

Note that after calling `ImportDocumentAsync` the code checks the asynchronous memory to
see when the background tasks are complete.

# Run the example

To run the example, either set the `OPENAI_API_KEY` environment variable with your
OpenAI API key, or adjust the memory builder code to use Azure or other LLMs.
