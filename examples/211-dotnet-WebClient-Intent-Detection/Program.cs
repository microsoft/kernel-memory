// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable CS8602 // memory is initialized before usage
#pragma warning disable CS0162 // unreachable code is managed via boolean settings

using System.Security.Cryptography;
using System.Text;
using Microsoft.KernelMemory;

public static class Program
{
    private static MemoryWebClient? s_memory;
    private static readonly List<string> s_toDelete = new();

    public static async Task Main()
    {
        s_memory = new MemoryWebClient("http://127.0.0.1:9001/");

        // =======================
        // === INGESTION =========
        // =======================

        await StoreIntent();

        // Wait for remote ingestion pipelines to complete
        foreach (var docId in s_toDelete)
        {
            while (!await s_memory.IsDocumentReadyAsync(documentId: docId))
            {
                Console.WriteLine("Waiting for memory ingestion to complete...");
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        // =======================
        // === Intent detection =========
        // =======================

        await AskForIntent("can I talk with someone?");
        await AskForIntent("visa payment");
        await AskForIntent("branch hours");
        await AskForIntent("I want to reset access");
        await AskForIntent("Check latest transactions");

        // =======================
        // === PURGE =============
        // =======================

        await DeleteMemories();
    }

    // =======================
    // === INGESTION =========
    // =======================

    // Adding intent as a tag with samples to memory.
    private static async Task StoreIntent()
    {
        var intentSamples = new Dictionary<string, List<string>>
        {
            {
                "FAQ", new List<string>
                {
                    "How do I check the balance on my bank Visa Debit Card?",
                    "Can I transfer money from my account to an account at another financial institution?",
                    "Can I join the bank online?",
                    "How do I find my tax statements?",
                    "Can I access my tax forms online?",
                    "Bank information",
                    "If I lose my debit or credit card, what should I do?",
                    "My card was lost/stolen, what should I do?",
                    "How do I activate my credit card?"
                }
            },

            {
                "account-balance", new List<string>
                {
                    "Tell me my account balance.",
                    "I'd like to know my account balance"
                }
            },

            {
                "transfer-funds", new List<string>
                {
                    "How to transfer fund to my checking account?",
                    "I need to pay my credit card",
                    "Can I pay my bill?",
                }
            },

            {
                "transaction-history", new List<string>
                {
                    "Can I get transaction records?",
                }
            },

            {
                "change-PIN", new List<string>
                {
                    "Update my password",
                    "How to change the PIN for my account?",
                    "Can you reset my PIN?"
                }
            },

            {
                "transfer-to-agent", new List<string>
                {
                    "Connect me to an agent",
                    "Transfer to an agent",
                    "Can I talk to a human?"
                }
            },
        };

        foreach (KeyValuePair<string, List<string>> intentSample in intentSamples)
        {
            foreach (string intentRequest in intentSample.Value)
            {
                var docId = HashThis(intentRequest);
                if (await s_memory.IsDocumentReadyAsync(docId))
                {
                    continue;
                }

                Console.WriteLine($"Uploading intent {intentSample.Key} with question: {intentRequest}");
                await s_memory.ImportTextAsync(intentRequest, tags: new TagCollection() { { "intent", intentSample.Key } }, documentId: docId);
                Console.WriteLine($"- Document Id: {docId}");
                s_toDelete.Add(docId);
            }
        }

        Console.WriteLine("\n====================================\n");
    }

    // =======================
    // === RETRIEVAL =========
    // =======================

    // Helper function to retrieve a tag value from a SearchResult
    private static string? GetTagValue(SearchResult answer, string tagName, string? defaultValue = null)
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

        // we ask for one chank of data with a minimum relevance of 0.75
        SearchResult answer = await s_memory.SearchAsync(request, minRelevance: 0.75, limit: 1);

        string? intent = GetTagValue(answer, "intent", "none");
        Console.WriteLine($"Intent: {intent}");

        Console.WriteLine("\n====================================\n");
    }

    private static string HashThis(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToUpperInvariant();
    }

    // =======================
    // === PURGE =============
    // =======================

    private static async Task DeleteMemories()
    {
        foreach (var docId in s_toDelete)
        {
            Console.WriteLine($"Deleting memories derived from {docId}");
            await s_memory.DeleteDocumentAsync(docId);
        }
    }
}
