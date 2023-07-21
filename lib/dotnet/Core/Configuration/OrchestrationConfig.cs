// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;

public class OrchestrationConfig
{
    public string Type { get; set; } = "InProcess";
    public DistributedPipelineConfig DistributedPipeline { get; set; } = new();
}
