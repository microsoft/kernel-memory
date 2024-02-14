---
nav_order: 2
grand_parent: Extensions
parent: Memory DBs
title: Qdrant
permalink: /extensions/memory-db/qdrant
layout: default
---
# Qdrant

[![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.MemoryDb.Qdrant)](https://www.nuget.org/packages/Microsoft.KernelMemory.MemoryDb.Qdrant/)

The [Qdrant](https://qdrant.tech) adapter allows to use Kernel Memory with Qdrant.

{: .note }
Qdrant record keys (point IDs) are limited to GUID or INT formats. Kernel Memory uses custom string
keys, adding to each point a custom "id" payload field used to identify records. This approach
requires one extra request during upsert operations, to obtain the point ID required.
