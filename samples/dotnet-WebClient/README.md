## Example: using the service with \<MemoryWebClient>

This example shows how to import multiple files and ask questions, delegating
the work to the Semantic Memory Service.

The example points to `https://127.0.0.1:9001` so you should start the
Semantic Memory Service locally before running this example.
Start `dotnet/Service/Service.csproj`. See `dotnet/Service/README.md` for details.

```csharp
var memory = new MemoryWebClient("http://127.0.0.1:9001/");

await memory.ImportFilesAsync(new[]
{
    new Document("file2.txt", new DocumentDetails("f02", "user1")),
    new Document("file3.docx", new DocumentDetails("f03", "user1")),
    new Document("file4.pdf", new DocumentDetails("f04", "user1")),
});

// ...wait for the service to import the files in the background...

string answer = await memory.AskAsync("What's Semantic Kernel?", "user1");
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