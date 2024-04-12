// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.Pipeline;

/// <summary>
/// DataPipeline representation.
/// Note: this object could be generalized to support any kind of pipeline, for now it's tailored
///       to specific design of SK memory indexer. You can use 'CustomData' to extend the logic.
/// </summary>
public sealed class DataPipeline
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ArtifactTypes
    {
        Undefined = 0,
        TextPartition = 1,
        ExtractedText = 2,
        TextEmbeddingVector = 3,
        SyntheticData = 4,
        ExtractedContent = 5,
    }

    public sealed class PipelineLogEntry
    {
        [JsonPropertyOrder(0)]
        [JsonPropertyName("t")]
        public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

        [JsonPropertyOrder(1)]
        [JsonPropertyName("src")]
        public string Source { get; set; }

        [JsonPropertyOrder(2)]
        [JsonPropertyName("txt")]
        public string Text { get; set; }

        public PipelineLogEntry(string source, string text)
        {
            this.Source = source;
            this.Text = text;
        }
    }

    public abstract class FileDetailsBase
    {
        /// <summary>
        /// Unique Id
        /// </summary>
        [JsonPropertyOrder(0)]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// File name
        /// </summary>
        [JsonPropertyOrder(1)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// File size
        /// </summary>
        [JsonPropertyOrder(2)]
        [JsonPropertyName("size")]
        public long Size { get; set; } = 0;

        /// <summary>
        /// File (MIME) type
        /// </summary>
        [JsonPropertyOrder(3)]
        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// File (MIME) type
        /// </summary>
        [JsonPropertyOrder(4)]
        [JsonPropertyName("artifact_type")]
        public ArtifactTypes ArtifactType { get; set; } = ArtifactTypes.Undefined;

        /// <summary>
        /// If the file is a partition, which partition number in the list of partitions extracted from a file.
        /// </summary>
        [JsonPropertyOrder(5)]
        [JsonPropertyName("partition_number")]
        public int PartitionNumber { get; set; } = 0;

        /// <summary>
        /// If the file is a partition, from which document page/audio segment/video scene is it from.
        /// </summary>
        [JsonPropertyOrder(6)]
        [JsonPropertyName("section_number")]
        public int SectionNumber { get; set; } = 0;

        /// <summary>
        /// File tags. Note, the data structure allows file tags to differ from the document tags.
        /// </summary>
        [JsonPropertyOrder(7)]
        [JsonPropertyName("tags")]
        public TagCollection Tags { get; set; } = new();

        /// <summary>
        /// List of handlers who have already processed this file
        /// </summary>
        [JsonPropertyOrder(17)]
        [JsonPropertyName("processed_by")]
        public List<string> ProcessedBy { get; set; } = new();

        /// <summary>
        /// Optional log describing how the file has been processed.
        /// The list is meant to contain only important details, avoiding excessive/verbose
        /// information that could affect the async queue performance.
        /// </summary>
        [JsonPropertyOrder(18)]
        [JsonPropertyName("log")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<PipelineLogEntry>? LogEntries { get; set; } = null;

        /// <summary>
        /// Check whether this file has already been processed by the given handler
        /// </summary>
        /// <param name="handler">Handler instance</param>
        /// <returns>True if the handler already processed the file</returns>
        public bool AlreadyProcessedBy(IPipelineStepHandler handler)
        {
            return this.ProcessedBy.Contains(handler.StepName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Mark the file as already processed by the given handler
        /// </summary>
        /// <param name="handler">Handler instance</param>
        public void MarkProcessedBy(IPipelineStepHandler handler)
        {
            this.ProcessedBy.Add(handler.StepName);
        }

        /// <summary>
        /// Add a new log entry, with some important information for the end user.
        /// DO NOT STORE PII OR SECRETS here.
        /// </summary>
        /// <param name="handler">Handler sending the information to log</param>
        /// <param name="text">Text to store for the end user</param>
        public void Log(IPipelineStepHandler handler, string text)
        {
            if (this.LogEntries == null)
            {
                this.LogEntries = new List<PipelineLogEntry>();
            }

            this.LogEntries.Add(new PipelineLogEntry(source: handler.StepName, text: text));
        }
    }

    public class GeneratedFileDetails : FileDetailsBase
    {
        /// <summary>
        /// Unique Id
        /// </summary>
        [JsonPropertyOrder(14)]
        [JsonPropertyName("parent_id")]
        public string ParentId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the partition used to generate this file (if the file is derived from a partition)
        /// </summary>
        [JsonPropertyOrder(15)]
        [JsonPropertyName("source_partition_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string SourcePartitionId { get; set; } = string.Empty;

        /// <summary>
        /// Deduplication hash used for consolidation tasks
        /// </summary>
        [JsonPropertyOrder(16)]
        [JsonPropertyName("content_sha256")]
        public string ContentSHA256 { get; set; } = string.Empty;
    }

    public class FileDetails : FileDetailsBase
    {
        /// <summary>
        /// List of files generated of the main file
        /// </summary>
        [JsonPropertyOrder(24)]
        [JsonPropertyName("generated_files")]
        public Dictionary<string, GeneratedFileDetails> GeneratedFiles { get; set; } = new();

        public string GetPartitionFileName(int partitionNumber)
        {
            return $"{this.Name}.partition.{partitionNumber}.txt";
        }

        public string GetHandlerOutputFileName(IPipelineStepHandler handler, int index = 0)
        {
            return $"{this.Name}.{handler.StepName}.{index}.txt";
        }
    }

    /// <summary>
    /// Index where the data ingestion pipeline is working.
    /// </summary>
    [JsonPropertyOrder(0)]
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    /// <summary>
    /// Id of the document and the pipeline instance.
    /// This value will persist throughout the execution and in the final data lineage used for citations.
    /// The value can be empty, e.g. when the pipeline is used to act on an entire index.
    /// </summary>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("document_id")]
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Unique execution id. If the pipeline is executed again, this value will change.
    /// A pipeline can be executed multiple time, e.g. to update a document, and each
    /// execution has a different ID, which is used for consolidation tasks.
    /// </summary>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("execution_id")]
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Full list of the steps in this pipeline.
    /// </summary>
    [JsonPropertyOrder(3)]
    [JsonPropertyName("steps")]
    public List<string> Steps { get; set; } = new();

    /// <summary>
    /// List of the steps remaining.
    /// </summary>
    [JsonPropertyOrder(4)]
    [JsonPropertyName("remaining_steps")]
    public List<string> RemainingSteps { get; set; } = new();

    /// <summary>
    /// List of steps already completed.
    /// </summary>
    [JsonPropertyOrder(5)]
    [JsonPropertyName("completed_steps")]
    public List<string> CompletedSteps { get; set; } = new();

    /// <summary>
    /// Document tags
    /// </summary>
    [JsonPropertyOrder(7)]
    [JsonPropertyName("tags")]
    public TagCollection Tags { get; set; } = new();

    [JsonPropertyOrder(8)]
    [JsonPropertyName("creation")]
    public DateTimeOffset Creation { get; set; } = DateTimeOffset.MinValue;

    [JsonPropertyOrder(9)]
    [JsonPropertyName("last_update")]
    public DateTimeOffset LastUpdate { get; set; }

    [JsonPropertyOrder(10)]
    [JsonPropertyName("files")]
    public List<FileDetails> Files { get; set; } = new();

    /// <summary>
    /// Unstructured dictionary available to support custom tasks and business logic.
    /// The orchestrator doesn't use this property, and it's up to custom handlers to manage it.
    /// </summary>
    [JsonPropertyOrder(20)]
    [JsonPropertyName("custom_data")]
    public Dictionary<string, object> CustomData { get; set; } = new();

    /// <summary>
    /// When uploading over an existing upload, we temporarily capture
    /// here the previous data, which could be a list in case of several
    /// concurrent updates. The data is eventually used to consolidate memory,
    /// e.g. deleting deprecated memory records. During the consolidation
    /// process the list is progressively emptied.
    /// </summary>
    [JsonPropertyOrder(21)]
    [JsonPropertyName("previous_executions_to_purge")]
    public List<DataPipeline> PreviousExecutionsToPurge { get; set; } = new();

    [JsonIgnore]
    public bool Complete => this.RemainingSteps.Count == 0;

    [JsonIgnore]
    public List<DocumentUploadRequest.UploadedFile> FilesToUpload { get; set; } = new();

    [JsonIgnore]
    public bool UploadComplete { get; set; }

    public DataPipeline Then(string stepName)
    {
        this.Steps.Add(stepName);
        return this;
    }

    public DataPipeline AddUploadFile(string name, string filename, string sourceFile)
    {
        return this.AddUploadFile(name, filename, File.ReadAllBytes(sourceFile));
    }

    public DataPipeline AddUploadFile(string name, string filename, byte[] content)
    {
        return this.AddUploadFile(name, filename, new BinaryData(content));
    }

    public DataPipeline AddUploadFile(string name, string filename, BinaryData content)
    {
        return this.AddUploadFile(name, filename, content.ToStream());
    }

    public DataPipeline AddUploadFile(string name, string filename, Stream content)
    {
        content.Seek(0, SeekOrigin.Begin);
        this.FilesToUpload.Add(new DocumentUploadRequest.UploadedFile(filename, content));
        return this;
    }

    public DataPipeline Build()
    {
        if (this.FilesToUpload.Count > 0)
        {
            this.UploadComplete = false;
        }

        this.RemainingSteps = this.Steps.Select(x => x).ToList();
        this.Creation = DateTimeOffset.UtcNow;
        this.LastUpdate = this.Creation;

        this.Validate();

        return this;
    }

    /// <summary>
    /// Change the pipeline to the next step, returning the name of the next step to execute.
    /// The name returned is used to choose the queue where the pipeline will be set.
    /// </summary>
    public string MoveToNextStep()
    {
        if (this.RemainingSteps.Count == 0)
        {
            throw new KernelMemoryException("The list of remaining steps is empty");
        }

        var stepName = this.RemainingSteps.First();
        this.RemainingSteps.RemoveAt(0);
        this.CompletedSteps.Add(stepName);

        return stepName;
    }

    /// <summary>
    /// Change the pipeline to the previous step, returning the name of the step to execute
    /// </summary>
    public string RollbackToPreviousStep()
    {
        if (this.CompletedSteps.Count == 0)
        {
            throw new KernelMemoryException("The list of completed steps is empty");
        }

        var stepName = this.CompletedSteps.Last();
        this.CompletedSteps.RemoveAt(this.CompletedSteps.Count - 1);
        this.RemainingSteps.Insert(0, stepName);

        return stepName;
    }

    public bool IsDocumentDeletionPipeline()
    {
        return this.Steps.Count == 1 && this.Steps.First() == Constants.PipelineStepsDeleteDocument;
    }

    public bool IsIndexDeletionPipeline()
    {
        return this.Steps.Count == 1 && this.Steps.First() == Constants.PipelineStepsDeleteIndex;
    }

    public void Validate()
    {
        if (string.IsNullOrEmpty(this.DocumentId))
        {
            // Rule exception: when deleting an index, the document ID is empty
            if (!this.IsIndexDeletionPipeline())
            {
                throw new ArgumentException("The pipeline ID is empty", nameof(this.DocumentId));
            }
        }

        if (string.IsNullOrEmpty(this.Index))
        {
            throw new ArgumentException("The index name is empty", nameof(this.Index));
        }

        string previous = string.Empty;
        foreach (string step in this.Steps)
        {
            if (string.IsNullOrEmpty(step))
            {
                throw new ArgumentException("The pipeline contains a step with empty name", nameof(this.Steps));
            }

            // This scenario is not allowed, to ensure execution consistency
            if (string.Equals(step, previous, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The pipeline contains two consecutive steps with the same name", nameof(this.Steps));
            }

            previous = step;
        }
    }

    public FileDetails GetFile(string id)
    {
        foreach (FileDetails file in this.Files)
        {
            if (file.Id == id) { return file; }
        }

        throw new OrchestrationException($"File '{id}' not found in the upload");
    }

    public DataPipelineStatus ToDataPipelineStatus()
    {
        return new DataPipelineStatus
        {
            Completed = this.Complete,
            Failed = false, // TODO
            Empty = this.Files.Count == 0,
            Index = this.Index,
            DocumentId = this.DocumentId,
            Tags = this.Tags,
            Creation = this.Creation,
            LastUpdate = this.LastUpdate,
            Steps = this.Steps,
            RemainingSteps = this.RemainingSteps,
            CompletedSteps = this.CompletedSteps,
        };
    }
}
