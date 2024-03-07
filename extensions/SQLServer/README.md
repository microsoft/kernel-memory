# Kernel Memory with Microsoft SQL Server

[![Nuget package](https://img.shields.io/nuget/v/KernelMemory.MemoryStorage.SqlServer)](https://www.nuget.org/packages/KernelMemory.MemoryStorage.SqlServer/)
[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/KMdiscord)

This folder contains tests for the [MS SQL Server](https://www.microsoft.com/sql-server) extension for Kernel Memory
available [here](https://github.com/kbeaugrand/SemanticKernel.Connectors.Memory.SqlServer).

Please note that the connector should not be confused with a NL2SQL feature, e.g. the ability to query the content
of a pre-existing SQL server. If you are looking for a solution that allows to import content from a SQL server and make
it searchable please see
[How to index data from Azure SQL in Azure AI Search](https://learn.microsoft.com/azure/search/search-howto-connecting-azure-sql-database-to-azure-search-using-indexers)

## Configuration

Configuration (appsettings.json):

```json
  // ...
    "SqlServer": {
      "ConnectionString": "...",
    }
  // ...
```

## Setup with Kernel Memory Builder / Dependency Injection

Method 1, simple applications:

```csharp
var sqlServerConfig = cfg.GetSection("Services:SqlServer").Get<SqlServerConfig>()!;
var memory = new KernelMemoryBuilder()
    .WithSqlServerMemoryDb(sqlServerConfig)
    // .WithOpenAI(openAiConfig)
    // .WithAzureOpenAITextGeneration(azureOpenAITextConfiguration)
    // .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfiguration)
    // .Build();
```

Method 2, injecting memory into an ASP.NET app:

```csharp
builder.Services.AddSingleton<IKernelMemory>(sp =>
{
    KernelMemoryBuilder kernelMemoryBuilder = new()
        .WithSqlServerMemoryDb(builder.Configuration.GetConnectionString("DefaultConnection"))
        //... 

    return kernelMemoryBuilder.Build<MemoryServerless>();
});
```

## KM Service setup

To run Kernel Memory service with SQL Server backend:

1. clone KM repository
2. add `KernelMemory.MemoryStorage.SqlServer` nuget to [Service.csproj](https://github.com/microsoft/kernel-memory/blob/main/service/Service/Service.csproj)
3. edit the part using `KernelMemoryBuilder` adding the same `.WithSqlServerMemoryDb(...)` call mentioned in the previous paragraph, e.g.
    ```csharp
   IKernelMemory memory = new KernelMemoryBuilder(appBuilder.Services)
    .FromAppSettings()
    .WithSqlServerMemoryDb(...)
    .Build();
    ```


## Local tests with Docker

You can test the connector locally with Docker:

```shell
docker pull mcr.microsoft.com/mssql/server:2022-latest

docker run -it -p 1433:1433 --rm -e "MSSQL_SA_PASSWORD=00_CHANGE_ME_00" -e "ACCEPT_EULA=Y" \
    mcr.microsoft.com/mssql/server:2022-latest
```

...using the following connection string:
```
Server=tcp:127.0.0.1,1433;Initial Catalog=master;Persist Security Info=False;User ID=sa;Password=00_CHANGE_ME_00;MultipleActiveResultSets=False;TrustServerCertificate=True;Connection Timeout=30;
```

For more information about the SQL Server Linux container:

- https://learn.microsoft.com/sql/linux/quickstart-install-connect-docker
- https://devblogs.microsoft.com/azure-sql/development-with-sql-in-containers-on-macos/
