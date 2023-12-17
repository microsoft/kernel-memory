## Example: .NET Minimal API to Ingest and Query Documents using <MemoryServerlessClient>

This example shows how to import multiple files and query them (ask questions), without
deploying the Kernel Memory Service by implementing a MemoryServerlessClient that runs as a .NET core minimal api.

All the logic is executed locally using the default C# handlers. In the current example the files are store in Azure Blob Storage but can be changes to local storage as well.

## Prepare the example

The configuration is done in the appsettings.json file. Either update the appsettings.json file directly or update the user secrets using the following commands:

>**Note**: It is not recommended to store secrets in the appsettings.json file. The appsettings.json file is checked into source control and should not contain secrets. The appsettings.json file is used for local development only. In production, the secrets should be stored in Azure Key Vault or in environment variables.

```sh
dotnet user-secrets set "KernelMemory:Services:AzureOpenAIText:Endpoint" "<REPLACE_WITH_YOUR_AZURE_OPENAI_TEXT_GENERATION_ENDPOINT>"
dotnet user-secrets set "KernelMemory:Services:AzureOpenAIText:APIKey" "<REPLACE_WITH_YOUR_AZURE_OPENAI_TEXT_GENERATION_API_KEY>"

dotnet user-secrets set "KernelMemory:Services:AzureOpenAIEmbedding:Endpoint" "<REPLACE_WITH_YOUR_AZURE_OPENAI_TEXT_EMBEDDING_ENDPOINT>"
dotnet user-secrets set "KernelMemory:Services:AzureOpenAIEmbedding:APIKey" "<REPLACE_WITH_YOUR_AZURE_OPENAI_TEXT_EMBEDDING_API_KEY>"

dotnet user-secrets set "KernelMemory:Services:AzureAISearchConfig:Endpoint" "<REPLACE_WITH_YOUR_AZURE_AI_SEARCH_ENDPOINT>"
dotnet user-secrets set "KernelMemory:Services:AzureAISearchConfig:APIKey" "<REPLACE_WITH_YOUR_AZURE_AI_SEARCH_APIKEY>"

dotnet user-secrets set "KernelMemory:Services:AzureBlobsConfig:Account" "<REPLACE_WITH_YOUR_AZURE_BLOB_STORAGE_ACCOUNT_NAME>"
dotnet user-secrets set "KernelMemory:Services:AzureBlobsConfig:AccountKey" "<REPLACE_WITH_YOUR_AZURE_BLOB_STORAGE_ACCOUNT_KEY>"
dotnet user-secrets set "KernelMemory:Services:AzureBlobsConfig:Container" "<REPLACE_WITH_YOUR_AZURE_BLOB_STORAGE_CONTAINER>"
```

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

## Run the example

To run the example, depending on your platform, execute either `run.sh` or `run.cmd`.

Once the api is running it would open swaggeer ui at http://localhost:5210/swagger/index.html
