// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DataFormats.WebPages;

public class MyWebScraper : IWebScraper
{
    public Task<WebScraperResult> GetContentAsync(string url, CancellationToken cancellationToken = default)
    {
        // Sample code
        Console.WriteLine($"Processing page {url} with {nameof(MyWebScraper)}...");

        // Your logic here
        var content = new BinaryData("...content page here...");

        // recommended: leave encoding out, include just the MIME/media type
        var contentType = "text/html";

        return Task.FromResult(new WebScraperResult
        {
            Content = content,
            ContentType = contentType,
            Success = true,
            Error = string.Empty
        });
    }
}

public static class Program
{
    public static async Task Main(string[] args)
    {
        var memory = new KernelMemoryBuilder()
            // .WithCustomWebScraper<MyWebScraper>()
            .WithOpenAIDefaults("no key")
            .Build();

        // Note: using custom "steps" to avoid LLM calls, and test just the custom web scraper
        await memory.ImportWebPageAsync(
            "https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md",
            steps: ["extract"]);
    }
}
