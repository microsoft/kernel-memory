// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;

namespace KernelMemory.Core.Config.Enums;

/// <summary>
/// Type of embeddings provider
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmbeddingsTypes
{
    /// <summary>Local Ollama instance</summary>
    Ollama,

    /// <summary>OpenAI API</summary>
    OpenAI,

    /// <summary>Azure OpenAI Service</summary>
    AzureOpenAI,

    /// <summary>Hugging Face Inference API</summary>
    HuggingFace
}
