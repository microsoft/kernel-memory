// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;

/// <summary>
/// OpenAI settings.
/// </summary>
public class OpenAIConfig
{
    public string Model { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
    public string OrgId { get; set; } = string.Empty;
}
