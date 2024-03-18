---
nav_order: 1
parent: Security
title: Security Filters
permalink: /security/filters
layout: default
---
# Security Filters

This document provides some guidance about how to organize your documents in
order to secure your data, e.g. making sure users can access only data
meant to be accessible to them.

Kernel Memory allows to organize memories with two main approaches, which
can also be used together for maximum flexibility.

1. Storing information in separate collections called "**Indexes**".
2. Labeling information with custom keywords called "**Tags**".

## Indexes

Depending on the storage engine, using multiple indexes can be expensive, so
we recommend using indexes only to scale horizontally, using your application
scalability metrics, such as the number of users, the number of projects,
the number of chats, and so on.

Currently, **indexes are completely isolated**, Kernel Memory doesn't allow
to search across indexes, so you should consider whether that's compatible
with your scenarios.

When uploading and searching, unless specified, Kernel Memory uses a
default index name, a single container for all the memories.

## Tags

When designing for security, Kernel Memory recommends using Tags, applying
to each document a User ID tag that your application can filter by.

**Vector DBs like Azure AI Search, Qdrant, Pinecone, etc. don't offer
record-level permissions** and search results can't vary by user.
Vector storages are optimized to store large quantity of documents indexed
using embedding vectors, and to quickly find similar documents.
Memory records stored in Vector DBs though can be decorated with metadata, and
can be filtered when searching, applying some logical filters.

Kernel Memory leverages this capability, and uses specific native filters
on all the supported Vector DBs (Azure AI Search, Qdrant, etc), removing
the need to learn ad-hoc filtering syntax, allowing to **tag every memory
during the ingestion**, and allowing to **filter by tag when searching**,
during the retrieval process.

Tags are free and customizable. Multiple tags can be used and each tag can
have multiple values. Tags can be used to filter by user, by type, etc. and
in particular can be leveraged for your security scenarios.

Here's some examples:

* Use a "userID" tag to restrict records to one or multiple users.
* Use a "userEmail" tag to restrict records using the user email address.
* Use a "groupID" tag to restrict records to groups defined externally.
* Use a "projectID" tag to assign records to a specific project.
* Use a "year" and "month" tags to filter records by time.

Kernel Memory uses the same Tagging feature to correlate memories to source
files, e.g. to generate citations, to handle document updates, and to apply
cascade deletions, etc.

> **Important:** Tags are not mandatory. When adding memories and when searching,
> so you should consider these two important points:
>
> 1. Memories stored without tags are visible only when searching without
     > filters, for instance they are visible when searching without specifying
     > a user ID.
> 2. Searching without filters searches the entire index. If you are using tags
     > as security filters, **you should always filter by tags when retrieving**
     > information.

## Code examples

Here's some example about how to use indexes and tags.

Simple file upload, without tags or explicit index name. The associated
memory records can't be filterable and are stored in the default index.

```csharp
// Upload a file into memory. This file has no tags.
var docId = await memory.ImportDocumentAsync("project.docx");

// Ask a question, without tags. This will search the entire index.
var answer = await memory.AskAsync("what's the project timeline?");
```

Simple file upload without tags, stored in a custom index. The associated
memory records can't be filtered by tags, but are isolated in a dedicated
index.

```csharp
// Upload a file in a specific index.
var docId = await memory.ImportDocumentAsync("project.docx", index: "index001");

// NO ANSWER: the data is not in the default index
var answer = await memory.AskAsync("what's the project timeline?");

// OK
var answer = await memory.AskAsync("what's the project timeline?", index: "index001");
```

### Security Filters

These examples use the `user` tag to secure data retrieval, making sure the
current user can see only data tagged by their user ID.

#### Example 1

File upload with a `user` tag. The associated memory records can be filtered
using the `user` tag.

Note that filters are not mandatory, so records are **visible also without
a filter**.

```csharp
var docId = await memory.ImportDocumentAsync(new Document()
                                                .AddFile("project.docx")
                                                .AddTag("user", "USER-333"));

// OK
var answer = await memory.AskAsync("what's the project timeline?");

// OK
var answer = await memory.AskAsync("what's the project timeline?",
                                    filter: MemoryFilters.ByTag("user", "USER-333"));

// NO ANSWER: memories are tagged with 'USER-333', so filter 'USER-444'
//            will not match the information extracted from project.docs
var answer = await memory.AskAsync("what's the project timeline?",
                                   filter: MemoryFilters.ByTag("user", "USER-444"));
```

#### Example 2

Very similar to previous example, using a specific index.

```csharp
// Upload a document in specific user and tag with user ID.
var docId = await memory.ImportDocumentAsync(new Document()
                                                .AddFile("project.docx")
                                                .AddTag("user", "USER-333"),
                                             index: "index002");

// NO ANSWER: the data is not in the default index
var answer = await memory.AskAsync("what's the project timeline?");

// NO ANSWER: even if the filter is correct, the data is not in the default index
var answer = await memory.AskAsync("what's the project timeline?",
                                   filter: MemoryFilters.ByTag("user", "USER-333"));

// OK
var answer = await memory.AskAsync("what's the project timeline?",
                                   filter: MemoryFilters.ByTag("user", "USER-333"),
                                   index: "index002");

// IMPORTANT: this command is missing the user tag and the service will return the data.
//            This is equivalent to an admin having full access.
var answer = await memory.AskAsync("what's the project timeline?",
                                   index: "index002");
```

#### Example 3

Example showing how to apply multiple tags, even for the same tag name.

In this case the document information is tagged with two user IDs,
so both users can ask for questions.

```csharp
// Upload file, allow two users to access
var docId = await memory.ImportDocumentAsync(new Document()
                                                .AddFile("project.docx")
                                                .AddTag("user", "USER-333")
                                                .AddTag("user", "USER-444"));

// OK: USER-333 tag matches
var answer = await memory.AskAsync("what's the project timeline?",
                                   filter: MemoryFilters.ByTag("user", "USER-333"));

// OK: USER-444 tag matches
var answer = await memory.AskAsync("what's the project timeline?",
                                   filter: MemoryFilters.ByTag("user", "USER-444"));
```

#### Example 4

Finally , tags can be used also for categorizing data:

```csharp
// Upload file, allow two users to access, and add a content type tag for extra filtering
var docId = await memory.ImportDocumentAsync(new Document()
                                                .AddFile("project.docx")
                                                .AddTag("user", "USER-333")
                                                .AddTag("user", "USER-444")
                                                .AddTag("type", "planning"));

// No information found, the type tag doesn't match
var answer = await memory.AskAsync("what's the project timeline?",
                                   filter: MemoryFilters.ByTag("user", "USER-333")
                                                        .ByTag("type", "email"));

// OK
var answer = await memory.AskAsync("what's the project timeline?",
                                   filter: MemoryFilters.ByTag("user", "USER-333")
                                                        .ByTag("type", "planning"));

```

# Security best practices

Summarizing, we recommend these best practices to secure Kernel Memory usage:

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

# Complex filters

When filtering memories it's possible to combine filters with `AND` and `OR` logic.
For instance, consider these scenarios:

1. Reply using memories belonging to "Taylor **OR** Andrea"
2. Reply using memories belonging to "Taylor **AND** Andrea"
3. Reply using "**News** belonging to **Taylor** AND **Blogs** belonging to **Andrea**"

## Using OR logic

Example:

> Reply using memories belonging to "Taylor **OR** Andrea"

Code:

```csharp
var answer = await memory.AskAsync(question,
                                   filters: new List<MemoryFilter>
                                   {
                                      MemoryFilters.ByTag("user", "Taylor"),
                                      // ... OR ...
                                      MemoryFilters.ByTag("user", "Andrea"),
                                   });
```

## AND vs OR syntax

Example:

> Reply using memories belonging to "Taylor **AND** Andrea"

Code:

```csharp
var answer = await memory.AskAsync(question,
                                   filters: new List<MemoryFilter>
                                   {
                                      MemoryFilters.ByTag("user", "Taylor")
                                                   // ... AND ...
                                                   .ByTag("user", "Andrea"),
                                   });
```

which can also be written more concisely as a single filter (using `filter` instead of `filters`):

```csharp
var answer = await memory.AskAsync(question,
                                   filter: MemoryFilters.ByTag("user", "Taylor")
                                                        // ... AND ...
                                                        .ByTag("user", "Andrea"));
```

## Using both AND and OR

Examples:

> Reply using "**News** belonging to **Taylor** AND **Blogs** belonging to **Andrea**"

In this case the "AND" is not strictly a logical AND asking to intersect two sets, but
an ask to merge (union) two results. As a result the sentence can be interpreted and
implemented in two different ways:

1. Ground the answer on memories that are both "news" **and** "blogs" **and** belong to both "Taylor" **and** "Andrea":

```csharp
var answer = await memory.AskAsync(question,
                                   filters: new List<MemoryFilter>
                                   {
                                      MemoryFilters.ByTag("user", "Taylor")
                                                   // ... AND ...
                                                   .ByTag("type", "News"),
                                                   // ... AND ...
                                                   .ByTag("user", "Andrea")
                                                   // ... AND ...
                                                   .ByTag("type", "Blog"),
                                   });
```

2. Ground the answer on memories that are "news owned by Taylor" **or** "blogs owned by Andrea":

```csharp
var answer = await memory.AskAsync(question,
                                   filters: new List<MemoryFilter>
                                   {
                                      MemoryFilters.ByTag("user", "Taylor")
                                                   // ... AND ...
                                                   .ByTag("type", "News"),
                                      // ... OR ...
                                      MemoryFilters.ByTag("user", "Andrea")
                                                   // ... AND ...
                                                   .ByTag("type", "Blog"),
                                   });
```

The latter is what users would expect. There are however several ways to ask a question, and
ultimately the logc depends on the language (English, Spanish, Portuguese, etc.) and the
user expectations.

For instance:

> Reply using "**News** written by **Taylor** using only **News** about **Space travel**"

all these conditions must be met:

* a memory must belong to Taylor
* a memory must be of type News
* a memory must be of type Space Travel

```csharp
var answer = await memory.AskAsync(question,
                                   filters: new List<MemoryFilter>
                                   {
                                      MemoryFilters.ByTag("type", "Taylor")
                                                   // ... AND ...
                                                   .ByTag("type", "News"),
                                                   // ... AND ...
                                                   .ByTag("type", "Space Travel"),
                                   });
```

And one last example:

> Reply using "**News** written by **Taylor** using only **News** about **Science** or **Space travel**"

which translates to these conditions:

* a memory must belong to Taylor
* a memory must be a News about Science, OR a News about Space travel

```csharp
var answer = await memory.AskAsync(question,
                                   filters: new List<MemoryFilter>
                                   {
                                      MemoryFilters.ByTag("user", "Taylor")
                                                   // ... AND ...
                                                   .ByTag("type", "News")
                                                   // ... AND ...
                                                   .ByTag("type", "Science"),
                                      // ... OR ...
                                      MemoryFilters.ByTag("user", "Taylor")
                                                   // ... AND ...
                                                   .ByTag("type", "News")
                                                   // ... AND ...
                                                   .ByTag("type", "Space travel"),
                                   });
```