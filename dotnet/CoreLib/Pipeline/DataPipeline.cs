// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticMemory.Client.Models;
using Microsoft.SemanticMemory.Core.Diagnostics;

namespace Microsoft.SemanticMemory.Core.Pipeline;

/// <summary>
/// DataPipeline representation.
/// Note: this object could be generalized to support any kind of pipeline, for now it's tailored
///       to specific design of SK memory indexer. You can use 'CustomData' to extend the logic.
/// </summary>
public class DataPipeline
{
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
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Check if this is an embedding file (checking the file extension)
        /// </summary>
        /// <returns>True if the file contains an embedding</returns>
        public bool IsEmbeddingFile()
        {
            return this.Type == MimeTypes.TextEmbeddingVector;
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
        /// Whether this is a partition/chunk/piece of the original content
        /// </summary>
        [JsonPropertyOrder(15)]
        [JsonPropertyName("is_partition")]
        public bool IsPartition { get; set; } = false;

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

        public bool IsAlreadyPartitioned()
        {
            string firstPartitionFileName = this.GetPartitionFileName(0);
            return (this.GeneratedFiles.ContainsKey(firstPartitionFileName));
        }

        public string GetPartitionFileName(int partitionNumber)
        {
            return $"{this.Name}.partition.{partitionNumber}.txt";
        }
    }

    /// <summary>
    /// Id of the pipeline instance. This value will persist throughout the execution and in the final data lineage used for citations.
    /// </summary>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

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

    [JsonPropertyOrder(6)]
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

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
    public List<IFormFile> FilesToUpload { get; set; } = new();

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
        this.FilesToUpload.Add(new FormFile(content, 0, content.Length, name, filename));
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

        return this;
    }

    /// <summary>
    /// Change the pipeline to the next step, returning the name of the next step to execute.
    /// The name returned is used to choose the queue where the pipeline will be set.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public string MoveToNextStep()
    {
        if (this.RemainingSteps.Count == 0)
        {
            throw new PipelineCompletedException("The list of remaining steps is empty");
        }

        var stepName = this.RemainingSteps.First();
        this.RemainingSteps = this.RemainingSteps.GetRange(1, this.RemainingSteps.Count - 1);
        this.CompletedSteps.Add(stepName);

        return stepName;
    }

    public void Validate()
    {
        if (string.IsNullOrEmpty(this.Id))
        {
            throw new ArgumentException("The pipeline ID is empty", nameof(this.Id));
        }

        if (string.IsNullOrEmpty(this.UserId))
        {
            throw new ArgumentException("The user ID is empty", nameof(this.UserId));
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
}
