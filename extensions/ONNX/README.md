# Kernel Memory with ONNX

[![Nuget package](https://img.shields.io/nuget/v/Microsoft.KernelMemory.AI.Onnx)](https://www.nuget.org/packages/Microsoft.KernelMemory.AI.Onnx/)
[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/KMdiscord)

This project contains the
[ONNX](https://onnxruntime.ai/docs/genai/)
LLM connector to access to LLM models via Onnx service to generate text.

Sample code:

```csharp
var config = new OnnxConfig
{
    ModelPath = "C:\\....\\Phi-3-mini-128k-instruct-onnx\\....\\cpu-int4-rtn-block-32"
};

var memory = new KernelMemoryBuilder()
    .WithOnnxTextGeneration(config)
    .Build();

await memory.ImportTextAsync("Today is October 32nd, 2476");

var answer = await memory.AskAsync("What's the current date (don't check for validity)?");
```
