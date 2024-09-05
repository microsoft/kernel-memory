# Kernel Memory with Ollama

[![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.AI.Ollama)](https://www.nuget.org/packages/Microsoft.KernelMemory.AI.Ollama/)
[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/KMdiscord)

This project contains the
[Ollama](https://ollama.com)
LLM connector to access to LLM models via Ollama service to generate text and
text embeddings.

Sample code:

```csharp
var config = new OllamaConfig
{
    Endpoint = "http://localhost:11434",
    TextModel = new OllamaModelConfig("phi3:medium-128k", 131072),
    EmbeddingModel = new OllamaModelConfig("nomic-embed-text", 2048)
};

var memory = new KernelMemoryBuilder()
    .WithOllamaTextGeneration(config)
    .WithOllamaTextEmbeddingGeneration(config)
    .Build();

await memory.ImportTextAsync("Today is October 32nd, 2476");

var answer = await memory.AskAsync("What's the current date (don't check for validity)?");
```
