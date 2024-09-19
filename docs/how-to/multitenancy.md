---
nav_order: 7
parent: How-to guides
title: Multitenancy
permalink: /how-to/multitenancy
layout: default
---

# Multitenancy in Kernel Memory

Multitenant architectures are commonly used in SaaS products and enterprises to achieve separation between tenants, customers, or even applications.
If your Kernel Memory workload will be used as a multitenant workload, you need to consider the following implementation.

{: .highlight }
To learn more about Architecting multitenant solutions please refer to [Azure Multitenant Architecture](https://learn.microsoft.com/azure/architecture/guide/multitenant/overview/)

Kernel Memory uses a rich tagging system to allow you to tag your data with any metadata you want.
This allows you to easily filter and query your data based on these tags.
When you are using Kernel Memory in a multitenant environment, you can use these tags to separate the data of different tenants.

## Data insertion example

```csharp
using Microsoft.KernelMemory;

// Connected to the memory service running locally
var memory = new MemoryWebClient("http://127.0.0.1:9001/");

// Import a web page
await memory.ImportWebPageAsync(
            "https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md",
            documentId: "doc02", tags: new TagCollection() { { "tenantId", "1" } });
// Import a text
var docId = await memory.ImportTextAsync("In physics, mass–energy equivalence is the relationship between mass and energy " +
    "in a system's rest frame, where the two quantities differ only by a multiplicative " +
    "constant and the units of measurement. The principle is described by the physicist " +
    "Albert Einstein's formula: E = m*c^2", tags: new TagCollection() { { "tenantId", "1" } });
// Import a file
var docId = await memory.ImportDocumentAsync("file1-Wikipedia-Carbon.txt", documentId: "doc001", tags: new TagCollection() { { "tenantId", "1" } });
// Import multiple files
var docId = await memory.ImportDocumentAsync(new Document("doc002")
    .AddFiles(["file2-Wikipedia-Moon.txt", "file3-lorem-ipsum.docx", "file4-KM-Readme.pdf"])
    .AddTag("tenantId", "1"));
```

Example above demonstrates how to insert data with a tag `tenantId` and assign it to a specific tenant with the value `1`.
This allows for easy data separation in a multitenant environment.

## Data retrieval example

```csharp
using Microsoft.KernelMemory;
// Connected to the memory service running locally
var memory = new MemoryWebClient("http://127.0.0.1:9001/");
// Ask a question
var answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("tenantId", "1"));
// Search for a document
var searchresult = await memory.SearchAsync(question, filter: MemoryFilters.ByTag("tenantId", "1"));
```

Example above demonstrates how to retrieve data with a tag `tenantId` that is equal to `1`.
This feature enables convenient data separation in a multitenant environment.
