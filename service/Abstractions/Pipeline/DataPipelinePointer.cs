// Copyright (c) Microsoft. All rights reserved.

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

    public DataPipelinePointer()
    {
    }

    public DataPipelinePointer(DataPipeline pipeline)
    {
        this.Index = pipeline.Index;
        this.DocumentId = pipeline.DocumentId;
    }
}
