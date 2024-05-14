## Example: using the service with \<MemoryWebClient>

This example shows how to import multiple files and ask questions, delegating
the work to the Kernel Memory Service.

The example points to `https://127.0.0.1:9001` so you should start the
Kernel Memory Service locally before running this example.
Start `dotnet/Service/Service.csproj`. See `dotnet/Service/README.md` for details.

```csharp
var memory = new MemoryWebClient("http://127.0.0.1:9001/");

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