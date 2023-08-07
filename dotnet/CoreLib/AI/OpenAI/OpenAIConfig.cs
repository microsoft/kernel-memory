// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.Core.AI.OpenAI;

/// <summary>
/// OpenAI settings.
/// </summary>
public class OpenAIConfig
{
    public string TextModel { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
    public string OrgId { get; set; } = string.Empty;
}
