# Kernel Memory with Elasticsearch

[![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.MemoryDb.Elasticsearch)](https://www.nuget.org/packages/Microsoft.KernelMemory.MemoryDb.Elasticsearch/)
[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/KMdiscord)

This folder contains tests for the [Elastisearch](https://www.elastic.co/) extension for Kernel Memory.

Configuration (appsettings.json):

```json
  // ...
    "Elasticsearch": {
        "Endpoint": "",
        "UserName": "",
        "CertificateFingerPrint": "",
        "Password": "",
    },
  // ...
```

You can test the connector locally with Docker:

```shell
docker run -it -p 9200:9200 -p 9300:9300 -e "discovery.type=single-node" --rm elasticsearch:8.11.3
```

The command should print on screen configuration details, such as fingerprint and default password. Copy
the values in `appsettings.development.json`. For example:

```json
  // ...
    "Elasticsearch": {
      "Endpoint": "https://localhost:9200",
      "UserName": "elastic",
      "CertificateFingerPrint": "b2ffe859bde01ece5734526a29b1ce7646b36030835cbbe81424a26151f5f2c5",
      "Password": "defg...."
    },
  // ...
```

For more information about the Elasticsearch extension:

- https://devblogs.microsoft.com/semantic-kernel/elasticsearch-kernelmemory
