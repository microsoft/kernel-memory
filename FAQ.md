# Semantic Memory F.A.Q.

### Is it possible to download web pages and turn the content into memory? Can I ask questions about the content of a web page?

Yes, the memory API includes a `ImportWebPageAsync` method that can be used to take
a web page content, and process the text content like files. Once the content is imported,
asking questions is very simple:

```csharp
var docId = await memory.ImportWebPageAsync("https://raw.githubusercontent.com/microsoft/semantic-memory/main/README.md");

var answer = await memory.AskAsync("Where can I store my semantic memory records?", MemoryFilters.ByDocument(docId));
```

![image](https://github.com/microsoft/semantic-memory/assets/371009/83d6487f-75f2-42d9-9ab5-ea6aed65231b)


### I've stored several documents in memory, how can I target a question to a specific document, getting answers grounded only on the selected doc?

When uploading a file (or multiple files), you can specify a document ID, or you can let
the service generate a document ID for you. You will see these Document IDs also when
getting answers. When sending a question, it's possible to **include a filter**, so it's
possible to filter by tags and **by document ID**. Here's an example:

```csharp
string docId = await memory.ImportDocumentAsync("manual.pdf");

await memory.ImportDocumentAsync("Europe.docx", documentId: "europe001");
```

In the first example ("manual.pdf"), the system will generate a new Document ID every time the code is executed,
and `docId` will contain the value, that you can save and use for questions.

In the second example ("book.docx"), the document ID is fixed, chosen by the client.

And this is the code showing how to ask a questions using only a specific document:

```csharp
var answer1 = await memory.AskAsync("What's the produc name?", MemoryFilters.ByDocument(docId));

var answer2 = await memory.AskAsync("What's the total population?", MemoryFilters.ByDocument("europe001"));
```

![image](https://github.com/microsoft/semantic-memory/assets/371009/18ea98ee-1210-498d-8513-56abc795ce4d)
