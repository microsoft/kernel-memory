// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.Core.Pipeline.Queue.AzureQueues;

public class AzureQueueConfig
{
    public string Auth { get; set; } = "";
    public string Account { get; set; } = "";
    public string EndpointSuffix { get; set; } = "core.windows.net";
    public string ConnectionString { get; set; } = "";
}
