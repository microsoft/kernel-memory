// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline.Queue;

public class AzureQueueConfig
{
    public string Auth { get; set; } = "";
    public string Account { get; set; } = "";
    public string EndpointSuffix { get; set; } = "core.windows.net";
    public string ConnectionString { get; set; } = "";
}
