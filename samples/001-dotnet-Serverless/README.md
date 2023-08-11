## Example: serverless, no deployment, using \<MemoryServerlessClient>

This example shows how to import multiple files and ask questions, without
deploying the Semantic Memory Service.

All the logic is executed locally using the default C# handlers. Depending
on your settings, files can be stored locally or in Azure Blobs.

```csharp
var memory = new Memory(serviceProvider);

await memory.ImportDocumentAsync(new Document(new[]
{
    "file2.txt",
    "file3.docx",
    "file4.pdf"
}, new DocumentDetails("user1", "doc002")));

string answer = await memory.AskAsync("user1", "What's Semantic Kernel?");
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