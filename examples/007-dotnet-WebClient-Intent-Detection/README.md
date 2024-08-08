## Example: using the service with \<MemoryWebClient>

This example shows how to import multiple files and ask questions, delegating
the work to the Kernel Memory Service.

### There are two ways to run this example:

1. The example points to `https://127.0.0.1:9001` so you should start the
   Kernel Memory Service locally before running this example.

2. You can easily deploy Kernel Memory to Azure by following procedure on [this page](../../infra/README.md).

Start `dotnet/Service/Service.csproj`. See `dotnet/Service/README.md` for details.

```csharp
var memory = new MemoryWebClient("http://127.0.0.1:9001/");
// In case you have deployed the service to Azure, you can use the following code:
// s_memory = new MemoryWebClient("https://AZURE_CONTAINER_APP_ENDPOINT.azurecontainerapps.io/", apiKey: "KEY_YOU_PROVIDED_WHEN_DEPLOYING");

await memory.ImportDocumentAsync(new Document("doc012")
    .AddFiles([ "file2.txt", "file3.docx", "file4.pdf" ])
    .AddTag("user", "Blake"));

while (!await memory.IsDocumentReadyAsync(documentId: "doc012"))
{
    Console.WriteLine("Waiting for memory ingestion to complete...");
    await Task.Delay(TimeSpan.FromSeconds(2));

string answer = await memory.AskAsync("What's Semantic Kernel?");
```

# Run the example

To run the example, depending on your platform, execute either `run.sh` or `run.cmd` or just `dotnet run`
