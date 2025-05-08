## Example: serverless, no deployment, using \<MemoryServerlessClient>

This example shows how to import multiple files and ask questions, without
deploying the Kernel Memory Service.

All the logic is executed locally using the default C# handlers. Depending
on your settings, files can be stored locally or in Azure Blobs.

```csharp
// Use the memory builder to customize credentials and dependencies
var memory = new KernelMemoryBuilder()
    .WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .Build<MemoryServerless>();

await memory.ImportDocumentAsync(new Document("doc012")
    .AddFiles([ "file2.txt", "file3.docx", "file4.pdf" ])
    .AddTag("user", "Blake"));

string answer = await memory.AskAsync("What's Semantic Kernel?");
```

# Prepare the example

Before running the code, create a `appsettings.Development.json` file (or edit `appsettings.json`),
overriding the values. The most important are endpoints and authentication details.

Note: the information stored in `appsettings.Development.json` are used only when
the env var `ASPNETCORE_ENVIRONMENT` is set to `Development`. Look at the
comments in `appsettings.json` for details and more advanced options.

You can find more details about the configuration options in `appsettings.json`,
and more info about .NET configurations at
https://learn.microsoft.com/aspnet/core/fundamentals/configuration

# Run the example

To run the example, execute `dotnet run` from this folder.