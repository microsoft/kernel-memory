---
nav_order: 90
has_children: false
title: F.A.Q.
permalink: /faq
layout: default
---
# Kernel Memory F.A.Q.

### How do I integrate Kernel Memory with my application?

There are two main modalities, **As a Service** and **Serverless**, plus
customizations you can apply.

1. Running Kernel Memory as a Service allows you to **interact with the memory
    via HTTP, in any language**. The repo contains a Memory Web client for .NET
    and some examples showing how to do the same from command line with `curl`.
    We will provide soon Web clients written in other languages.
    
    Kernel Memory Service is designed to run as an internal service, behind
    your backend, similarly to a DB, so you should not expose the service to
    public traffic without authenticating your users first, similar to a typical
    backend integrated with a SQL server, Service Bus, etc.

    One important benefit of the service, the solution can scale horizontally
    and can support long running operation reliably using durable queues.

    For more details, see the [service documentation](../dotnet/Service/README.md).

    [Here](../examples/002-dotnet-WebClient/README.md) you can find an example
    showing the web client interacting with the service.

2. Alternatively, you can **embed Kernel Memory directly into your .NET
    applications**, using the **Serverless Memory client**. This is limited to
    .NET applications and doesn't allow mixing .NET pipelines with pipeline
    handlers written in Python or TypeScript. The serverless approach can be
    very useful to create console applications, run tests and demos.
    
    **The serverless memory API is the same** offered by the service, so it's
    possible to switch from Service to Serverless changing only few lines code.

    [Here](../examples/001-dotnet-Serverless/README.md) you can find an example
    showing the serverless client.

![image](https://github.com/microsoft/kernel-memory/assets/371009/83d6487f-75f2-42d9-9ab5-ea6aed65231b)

### How do I protect users information, e.g. isolating data and making sure users cannot access reserved information?

In order to protect users data, you should follow these design principles:

* Use Kernel Memory as **a private backend component**, similar to a SQL
  Server, without granting direct access. When using Kernel Memory as a
  service, consider assigning the service a reserved IP, accessible only to
  your services, and using HTTPS only.
* Authenticate users in your backend using a secure solution like Azure
  Active Directory, extract the user ID from the signed credentials like JWT
  tokens or client certs, and tag every interaction with Kernel Memory with
  this User ID
* **Use Kernel Memory Tags as Security Filters**. Make sure every API call
  to Kernel Memory uses a User tag, both when reading and writing to memory.
  See [Security Filters](security/filters) for more details.

![Network diagram](network.png)

![image](https://github.com/microsoft/kernel-memory/assets/371009/83d6487f-75f2-42d9-9ab5-ea6aed65231b)

### Is it possible to download web pages and turn the content into memory? Can I ask questions about the content of a web page?

Yes, the memory API includes an `ImportWebPageAsync` method that can be used
to take a web page content, and process the text content like files. Once
the content is imported, asking questions is very simple:

```csharp
// Import memories from a web page
var docId = await memory.ImportWebPageAsync(
    "https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md");

// Answer questions using the page content to ground the answer
var answer = await memory.AskAsync("Where can I store my kernel memory records?",
                                   MemoryFilters.ByDocument(docId));
```

![image](https://github.com/microsoft/kernel-memory/assets/371009/83d6487f-75f2-42d9-9ab5-ea6aed65231b)

### I've stored several documents in memory, how can I target a question to a specific document, getting answers grounded only on the selected doc?

When uploading a file (or multiple files), you can specify a document ID,
or you can let the service generate a document ID for you. You will see these
Document IDs also when getting answers. 

When sending a question, it's possible to **include a filter**, so it's possible
to filter by tags and **by document ID**.

Here's an example:

```csharp
string docId = await memory.ImportDocumentAsync("manual.pdf");

await memory.ImportDocumentAsync("Europe.docx", documentId: "europe001");
```

In the first example ("manual.pdf"), the system will generate a new Document ID
every time the code is executed, and `docId` will contain the value, that you
can save and use for questions.

In the second example ("book.docx"), the document ID is fixed, chosen by the
client.

And this is the code showing how to ask a questions using only a specific
document:

```csharp
var answer1 = await memory.AskAsync("What's the product name?",
                                    MemoryFilters.ByDocument(docId));

var answer2 = await memory.AskAsync("What's the total population?",
                                    MemoryFilters.ByDocument("europe001"));
```

![image](https://github.com/microsoft/kernel-memory/assets/371009/18ea98ee-1210-498d-8513-56abc795ce4d)

If you have any question, please do not hesitate to
[open a new issue](https://github.com/microsoft/kernel-memory/issues/new)
in the Kernel Memory repository. Thanks!
