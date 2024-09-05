// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.AI.Ollama;

public class OllamaConfig
{
    /// <summary>
    /// Ollama HTTP endpoint.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Settings for the model used for text generation. Chat models can be used too.
    /// </summary>
    public OllamaModelConfig TextModel { get; set; } = new OllamaModelConfig();

    /// <summary>
    /// Settings for the model used for text embedding generation.
    /// </summary>
    public OllamaModelConfig EmbeddingModel { get; set; } = new OllamaModelConfig();
}
