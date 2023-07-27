// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.Core.Configuration;

/// <summary>
/// Azure OpenAI settings.
/// </summary>
public class AzureOpenAIConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string Deployment { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
}
