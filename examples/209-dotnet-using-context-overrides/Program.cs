// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Context;

public static class Program
{
    private static readonly IKernelMemory s_memory = new MemoryWebClient("http://127.0.0.1:9001/");

    public static async Task Main()
    {
        await AnswerOnly20Token();
        await AnswerUsingHighTemperature();
        await AnswerUsingACustomRAGPrompt();
        await CustomAnswerShowingOnlyTags();
    }

    private static async Task AnswerOnly20Token()
    {
        Console.WriteLine("=========================================================");
        var context = new RequestContext();

        // Truncate the answer after 20 tokens
        context.SetArg("custom_rag_max_tokens_int", 20);

        var answer = await s_memory.AskAsync("In few words, what's Kernel Memory?", minRelevance: 0, context: context);
        Console.WriteLine($"\nAnswer: {answer.Result}");
    }

    private static async Task AnswerUsingHighTemperature()
    {
        Console.WriteLine("=========================================================");
        var context = new RequestContext();

        // Truncate the answer after 50 tokens
        context.SetArg("custom_rag_max_tokens_int", 50);

        // Allow the LLM to generate random answers
        context.SetArg("custom_rag_temperature_float", 2.0);

        var answer = await s_memory.AskAsync("What's the most relevant fact about KM?", minRelevance: 0, context: context);
        Console.WriteLine($"\nAnswer: {answer.Result}");
    }

    private static async Task AnswerUsingACustomRAGPrompt()
    {
        Console.WriteLine("=========================================================");
        var context = new RequestContext();

        // Use a custom template for facts
        context.SetArg("custom_rag_fact_template_str", "=== Last update: {{$meta[last_update]}} ===\n{{$content}}\n");

        // Use a custom RAG prompt
        context.SetArg("custom_rag_prompt_str", """
                                                Facts:
                                                {{$facts}}
                                                ======
                                                Given only the timestamped facts above, provide a very short answer, include the relevant dates in brackets.
                                                If you don't have sufficient information, reply with '{{$notFound}}'.
                                                Question: {{$input}}
                                                Answer:
                                                """);

        var answer = await s_memory.AskAsync("What's Kernel Memory?", minRelevance: 0.5, context: context);
        Console.WriteLine($"\nAnswer: {answer.Result}");

        /* Output:

             Answer: Kernel Memory (KM) is a multi-modal AI Service specialized in the efficient indexing of datasets through custom continuous
             data hybrid pipelines, with support for Retrieval Augmented Generation (RAG), synthetic memory, prompt engineering, and custom semantic
             memory processing. It is designed for integration with Semantic Kernel, Microsoft Copilot, and ChatGPT, enhancing data-driven features
             in applications for popular AI platforms. KM is available as a web service, Docker container, .NET library, and Plugin. It is not an
             officially supported Microsoft offering but serves as a demonstration of best practices and reference architecture for memory in AI
             and LLMs application scenarios [2024-06-17].
         */
    }

    private static async Task CustomAnswerShowingOnlyTags()
    {
        Console.WriteLine("=========================================================");
        var context = new RequestContext();

        // Use a custom template for facts
        context.SetArg("custom_rag_fact_template_str", """
                                                       * Source: {{$source}}
                                                         - Relevance score: {{$relevance}}
                                                         - Tags: {{$tags}}
                                                       """);

        // Use a custom RAG prompt
        context.SetArg("custom_rag_prompt_str", """
                                                Sources:
                                                {{$facts}}
                                                ======
                                                List non empty tags in JSON format.
                                                """);

        var answer = await s_memory.AskAsync("What's Kernel Memory?", minRelevance: 0.7, context: context);
        Console.WriteLine($"\nAnswer: {answer.Result}");

        /* Output:

             Answer: ```json
               [
                 {
                   "tag": "user:Blake"
                 },
                 {
                   "tag": "user:Taylor",
                   "collection": ["meetings", "NASA", "space"],
                   "type": "news"
                 }
               ]
               ```
         */
    }
}
