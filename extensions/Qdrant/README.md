# Kernel Memory with Qdrant

[![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.MemoryDb.Qdrant)](https://www.nuget.org/packages/Microsoft.KernelMemory.MemoryDb.Qdrant/)
[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/KMdiscord)

This project contains the [Qdrant](https://qdrant.tech) adapter allowing to use Kernel Memory with Qdrant.

Note: Qdrant record keys (point IDs) are limited to GUID or INT formats. Kernel Memory uses custom string
keys, adding to each point a custom "id" payload field used to identify records. This approach
requires one extra request during upsert operations, to obtain the point ID required.
