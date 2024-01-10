# Kernel Memory with Microsoft SQL Server

[![Nuget package](https://img.shields.io/nuget/v/KernelMemory.MemoryStorage.SqlServer)](https://www.nuget.org/packages/KernelMemory.MemoryStorage.SqlServer/)
[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/KMdiscord)

This project contains the [MS SQL Server](https://www.microsoft.com/sql-server) adapter allowing
to use Kernel Memory with MS SQL Server.

Configuration (appsettings.json):

```json
  // ...
    "SqlServer": {
      "ConnectionString": "...",
    }
  // ...
```

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
