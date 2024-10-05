---
nav_order: 5
parent: How-to guides
title: Intent Detection
permalink: /how-to/intent-detection
layout: default
---

# Intent Detection

Intent detection, also known as intent recognition, is a natural language processing (NLP) technique used to determine the purpose or goal behind a user's input.
This technology is crucial in applications like e-commerce and call centers, where understanding customer needs accurately can significantly enhance user experience.
For instance, in an e-store, intent detection can analyze customer queries such as "I need a new phone" or "Show me the latest laptops" and categorize them under relevant product categories like "Smartphones" or "Laptops."
This ensures that users receive precise and timely recommendations.
Similarly, in a call center, intent detection can help in understanding the main topic of a customer's call, whether it's a billing issue, technical support, or a service inquiry.
By recognizing these intents, the system can route the call to the appropriate department or provide automated responses, thereby improving efficiency and customer satisfaction.

{: .highlight }
Many customers rely on Language Understanding (LUIS) for intent detection [Intents](https://learn.microsoft.com/en-us/azure/ai-services/luis/concepts/intents).
Important to note that LUIS will be retired on October 1st 2025 and starting April 1st 2023 you will not be able to create new LUIS resources.

Kernel Memory can help with intent detection leveraging Taging feature.
You can tag your data or chanks of text with a tag metadata.
Leveraging vector similarity search you can find the most similar data to the input text and get the tag of the most similar data.
This way you can detect the intent of the input text.

## How to use Kernel Memory for Intent Detection

### Data insertion example

```csharp
using Microsoft.KernelMemory;

// Connected to the memory service running locally
var memory = new MemoryWebClient("http://127.0.0.1:9001/");

// add to memory "Update my password" with tag "intent" and value "change-PIN"
await memory.ImportTextAsync("Update my password", tags: new TagCollection() { { "intent", "change-PIN" } });
```

Example above demonstrates how to insert data with a tag `tenantId` and assign it to a specific tenant with the value `1`.
This allows for easy data separation in a multitenant environment.

### Intent Detection example

```csharp
using Microsoft.KernelMemory;

// Connected to the memory service running locally
var memory = new MemoryWebClient("http://127.0.0.1:9001/");
await AskForIntent("I want to reset access");

// Helper function to retrieve a tag value from a SearchResult
private static string? getTagValue(SearchResult answer, string tagName, string? defaultValue = null)
{
    if (answer.Results.Count == 0)
    {
        return defaultValue;
    }
    Citation citation = answer.Results[0];
    if (citation == null)
    {
        return defaultValue;
    }
    if (citation.Partitions[0].Tags.ContainsKey(tagName))
    {
        return citation.Partitions[0].Tags[tagName][0];
    }
    return defaultValue;
}

private static async Task AskForIntent(string request)
{
    Console.WriteLine($"Question: {request}");
    // // we ask for one chank of data with a minimum relevance of 0.75
    SearchResult searchR = await s_memory.SearchAsync(request, minRelevance: 0.75, limit: 1);
    string? intent = getTagValue(searchR, "intent", "none");
    Console.WriteLine($"Intent: {intent}");
}
```

You can also review [Intent Detection working example](https://github.com/microsoft/kernel-memory/tree/main/examples/211-dotnet-WebClient-Intent-Detection/README.md)
