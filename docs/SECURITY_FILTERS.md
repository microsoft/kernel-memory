# Security Filters

Semantic Memory allows to organize memories with two main approaches, which
can also be used together for maximum flexibility.

1. Storing data in separate collections called "**Indexes**".
2. Labeling data with custom keywords called "**Tags**".

## Indexes

Depending on the storage engine, using multiple indexes can be expensive, so
we recommend using indexes only to scale horizontally, using your application
scalability metrics, such as the number of users, the number of projects,
the number of chats, and so on.

Currently, **indexes are completely isolated**, Semantic Memory doesn't allow
to search across indexes, so you should consider whether that's compatible
with your scenarios.

When uploading and searching, unless specified, Semantic Memory uses a
default index name, a single container for all the memories.

## Tags

When designing for security, Semantic Memory recommends using Tags, applying
to each document a User ID tag that your application can filter by.

**Vector DBs like Azure Cognitive Search, Qdrant, Pinecone, etc. don't offer
document-level permissions** and search results can't vary by user.
Documents stored in Vector DBs though can be decorated with metadata and can
be filtered when searching, applying some filters.

Semantic Memory makes this transparent regardless of the vector DB selected,
allowing to **tag every memory during the ingestion**, and allowing to **filter
by tag when searching**, during the retrieval process.

Tags are completely free and customizable. Multiple tags can be used and each
tag can have multiple values, so you can create your custom security filters.
Here's some example scenarios:

* Use a "userID" tag to restrict records to one or multiple users.
* Use a "userEmail" tag to restrict records using the user email address.
* Use a "groupID" tag to restrict records to groups defined externally.
* Use a "projectID" tag to assign records to a specific project.
* Use a "year" and "month" tags to filter records by time.

Semantic Memory uses the same Tagging feature to correlate memories to source
files, e.g. to generate citations, to handle document updates, and to apply
cascade deletions, etc.

> **Important:** Tags are not mandatory. When adding memories and when searching,
> so you should consider these two important points:
>
> 1. Memories stored without tags are visible only when searching without
     > filters, e.g. they are visible to all users.
>2. Searching without filters searches the entire index. If you are using tags
    > as security filters, **you should always filter by tags when retrieving**
    > information.

## Code examples

Here's some example about how to use indexes and tags.

Simple file upload, without tags or explicit index name. The associated
memory records can't be filterable and are stored in the default index.

```csharp
var docId = await memory.ImportDocumentAsync("project.docx");
 
var answer = await memory.AskAsync("what's the project timeline?");
```

Simple file upload without tags, stored in a custom index. The associated
memory records can't be filtered by tags, but are isolated in a dedicated
index.

```csharp
var docId = await memory.ImportDocumentAsync("project.docx", index: "index001");

// NO ANSWER: the data is not in the default index
var answer = await memory.AskAsync("what's the project timeline?");

// OK
var answer = await memory.AskAsync("what's the project timeline?", index: "index001");
```

File upload with a `user` tag. The associated memory records can be filtered
using the `user` tag.

Note that filters are not mandatory, so the records are **visible also without
a filter**.

```csharp
var docId = await memory.ImportDocumentAsync(new Document()
    .AddFile("project.docx")
    .AddTag("user", "USER-333"));

// OK
var answer = await memory.AskAsync("what's the project timeline?");

// OK
var answer = await memory.AskAsync("what's the project timeline?", MemoryFilters.ByTag("user", "USER-333"));

// NO ANSWER: memories are tagged with 'USER-333', so filter 'USER-444' will not match the information extracted from project.docs
var answer = await memory.AskAsync("what's the project timeline?", MemoryFilters.ByTag("user", "USER-444"));
```

Very similar to previous example, using a specific index.

```csharp
var docId = await memory.ImportDocumentAsync(new Document()
        .AddFile("project.docx")
        .AddTag("user", "USER-333"),
    index: "index002");

// NO ANSWER: the data is not in the default index
var answer = await memory.AskAsync("what's the project timeline?");

// NO ANSWER: the data is not in the default index
var answer = await memory.AskAsync("what's the project timeline?", MemoryFilters.ByTag("user", "USER-333"));

// OK
var answer = await memory.AskAsync("what's the project timeline?", MemoryFilters.ByTag("user", "USER-333"), index: "index002");
```

Example showing how to apply multiple tags, even for the same tag name.
E.g. in this case the document information is tagged with two user IDs,
so both users can ask for questions.

```csharp
var docId = await memory.ImportDocumentAsync(new Document()
    .AddFile("project.docx")
    .AddTag("user", "USER-333")
    .AddTag("user", "USER-444"));

// OK
var answer = await memory.AskAsync("what's the project timeline?", MemoryFilters.ByTag("user", "USER-333"));

// OK
var answer = await memory.AskAsync("what's the project timeline?", MemoryFilters.ByTag("user", "USER-444"));
```

Finally , tags can be used also for categorizing data:

```csharp
var docId = await memory.ImportDocumentAsync(new Document()
    .AddFile("project.docx")
    .AddTag("user", "USER-333")
    .AddTag("user", "USER-444")
    .AddTag("type", "planning"));

// No information found, the type tag doesn't match
var answer = await memory.AskAsync("what's the project timeline?", MemoryFilters.ByTag("user", "USER-333").ByTag("type", "email"));

// OK
var answer = await memory.AskAsync("what's the project timeline?", MemoryFilters.ByTag("user", "USER-333").ByTag("type", "planning"));

```