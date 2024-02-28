## Example: serverless, custom ingestion pipeline

This is a more advanced example, showing how to customize the way documents
are processed and turned into memories when using the serverless memory synchronous pipeline.

The example uses `memory.Orchestrator.AddHandler` to set which handlers are available:

```csharp
memory.Orchestrator.AddHandler<TextExtractionHandler>("extract_text");
memory.Orchestrator.AddHandler<TextPartitioningHandler>("split_text_in_partitions");
memory.Orchestrator.AddHandler<GenerateEmbeddingsHandler>("generate_embeddings");
memory.Orchestrator.AddHandler<SummarizationHandler>("summarize");
memory.Orchestrator.AddHandler<SaveRecordsHandler>("save_memory_records");
```

and the `steps` parameter to choose which handlers to use
during the ingestion:

```csharp
await memory.ImportDocumentAsync(
    new Document("inProcessTest")
        .AddFile("file1-Wikipedia-Carbon.txt")
        .AddFile("file2-Wikipedia-Moon.txt")
        .AddFile("file3-lorem-ipsum.docx")
        .AddFile("file4-KM-Readme.pdf")
        .AddFile("file5-NASA-news.pdf")
        .AddTag("testName", "example3"),
    index: "user-id-1",
    steps: new[]
    {
        "extract_text",
        "split_text_in_partitions",
        "generate_embeddings",
        "save_memory_records"
    });
```

Note that as soon as `ImportDocumentAsync` is done, the memories are available for queries.

# Run the example

To run the example, either set the `OPENAI_API_KEY` environment variable with your
OpenAI API key, or adjust the memory builder code to use Azure or other LLMs.
