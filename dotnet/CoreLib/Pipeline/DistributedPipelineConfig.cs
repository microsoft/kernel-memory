// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.Pipeline.Queue;

namespace Microsoft.SemanticMemory.Core.Pipeline;

public class DistributedPipelineConfig
{
    public string Type { get; set; } = "rabbitmq";
    public AzureQueueConfig AzureQueue { get; set; } = new();
    public RabbitMqConfig RabbitMq { get; set; } = new();
    public FileBasedQueueConfig FileBasedQueue { get; set; } = new();
}
