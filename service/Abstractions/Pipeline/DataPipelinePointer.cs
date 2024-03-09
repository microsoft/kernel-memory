// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.Pipeline;

public sealed class DataPipelinePointer
{
    /// <summary>
    /// Index where the data ingestion pipeline is working.
    /// </summary>
    [JsonPropertyOrder(0)]
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    /// <summary>
    /// Id of the document and the pipeline instance.
    /// </summary>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("document_id")]
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Id of the pipeline execution. When updating a document a new execution ID is generated,
    /// and potential work left on the previous execution is abandoned.
    /// </summary>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("execution_id")]
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// List of all steps to be executed. Having a copy of the list allows to better handle
    /// concurrent operations and scenarios where the pipeline file is corrupted/lost.
    /// </summary>
    [JsonPropertyOrder(3)]
    [JsonPropertyName("steps")]
    public List<string> Steps { get; set; } = new();

    public DataPipelinePointer()
    {
    }

    public DataPipelinePointer(DataPipeline pipeline)
    {
        this.Index = pipeline.Index;
        this.DocumentId = pipeline.DocumentId;
        this.ExecutionId = pipeline.ExecutionId;
        this.Steps = pipeline.Steps;
    }
}
