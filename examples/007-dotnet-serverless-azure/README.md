## Example: serverless, no deployment, using \<MemoryServerlessClient>

This example shows how to import multiple files and ask questions, without
deploying the Kernel Memory Service, leveraging all Azure services:

- [Azure Blobs](https://learn.microsoft.com/azure/storage/blobs/storage-blobs-introduction): used to store files.
- [Azure AI Document Intelligence](https://azure.microsoft.com/products/ai-services/ai-document-intelligence): used to extract text from images.
- [Azure OpenAI](https://azure.microsoft.com/products/ai-services/openai-service): used to index data with embeddings and to generate answers.
- [Azure AI Search](https://learn.microsoft.com/azure/search/search-what-is-azure-search): used to store embeddings and chunks of text.
- [Azure AI Content Safety](https://azure.microsoft.com/products/ai-services/ai-content-safety): validate LLM output to avoid unsafe content.

For each service, you can find and configure settings in [appsettings.json](appsettings.json).

The example runs a couple of memory ingestions and ask questions verifying the end to end flow, see the code in [Program.cs](Program.cs).

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