// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory;

public static class Constants
{
    public static class WebService
    {
        // Form field/Query param containing the Index Name
        public const string IndexField = "index";

        // Form field/Query param containing the Document ID
        public const string DocumentIdField = "documentId";

        // Form field/Query param containing the Filename
        public const string FilenameField = "filename";

        // Form field containing the list of tags
        public const string TagsField = "tags";

        // Form field containing the list of pipeline steps
        public const string StepsField = "steps";

        // Form field containing the optional arguments JSON string
        public const string ArgsField = "args";
    }

    public static class CustomContext
    {
        public static class Partitioning
        {
            // Used to override MaxTokensPerParagraph config
            public const string MaxTokensPerParagraph = "custom_partitioning_max_tokens_per_paragraph_int";

            // Used to override OverlappingTokens config
            public const string OverlappingTokens = "custom_partitioning_overlapping_tokens_int";

            // Used to prepend header to each partition
            public const string ChunkHeader = "custom_partitioning_chunk_header_str";
        }

        public static class EmbeddingGeneration
        {
            // Used to override MaxBatchSize embedding generators config
            public const string BatchSize = "custom_embedding_generation_batch_size_int";
        }

        public static class Rag

        {
            // Used to override No Answer config
            public const string EmptyAnswer = "custom_rag_empty_answer_str";

            // Used to override the RAG prompt
            public const string Prompt = "custom_rag_prompt_str";

            // Used to override how facts are injected into RAG prompt
            public const string FactTemplate = "custom_rag_fact_template_str";

            // Used to override the max tokens to generate when using the RAG prompt
            public const string MaxTokens = "custom_rag_max_tokens_int";

            // Used to override the temperature (default 0) used with the RAG prompt
            public const string Temperature = "custom_rag_temperature_float";

            // Used to override the nucleus sampling probability (default 0) used with the RAG prompt
            public const string NucleusSampling = "custom_rag_nucleus_sampling_float";
        }

        public static class Summary
        {
            // Used to override the summarization prompt
            public const string Prompt = "custom_summary_prompt_str";

            // Used to override the size of the summary
            public const string TargetTokenSize = "custom_summary_target_token_size_int";

            // Used to override the number of overlapping tokens
            public const string OverlappingTokens = "custom_summary_overlapping_tokens_int";
        }
    }

    // // Default User ID owning documents uploaded without specifying a user
    // public const string DefaultDocumentOwnerUserId = "defaultUser";

    // Internal file used to track progress of asynchronous pipelines
    public const string PipelineStatusFilename = "__pipeline_status.json";

    // Tags settings
    public const char ReservedEqualsChar = ':';
    public const string ReservedTagsPrefix = "__";

    // Tags reserved for internal logic
    public const string ReservedDocumentIdTag = $"{ReservedTagsPrefix}document_id";
    public const string ReservedFileIdTag = $"{ReservedTagsPrefix}file_id";
    public const string ReservedFilePartitionTag = $"{ReservedTagsPrefix}file_part";
    public const string ReservedFilePartitionNumberTag = $"{ReservedTagsPrefix}part_n";
    public const string ReservedFileSectionNumberTag = $"{ReservedTagsPrefix}sect_n";
    public const string ReservedFileTypeTag = $"{ReservedTagsPrefix}file_type";
    public const string ReservedSyntheticTypeTag = $"{ReservedTagsPrefix}synth";

    // Known tags
    public const string TagsSyntheticSummary = "summary";

    // Properties stored inside the payload
    public const string ReservedPayloadSchemaVersionField = "schema";
    public const string ReservedPayloadTextField = "text";
    public const string ReservedPayloadFileNameField = "file";
    public const string ReservedPayloadUrlField = "url";
    public const string ReservedPayloadLastUpdateField = "last_update";
    public const string ReservedPayloadVectorProviderField = "vector_provider";
    public const string ReservedPayloadVectorGeneratorField = "vector_generator";

    // Endpoints
    public const string HttpAskEndpoint = "/ask";
    public const string HttpSearchEndpoint = "/search";
    public const string HttpDownloadEndpoint = "/download";
    public const string HttpUploadEndpoint = "/upload";
    public const string HttpUploadStatusEndpoint = "/upload-status";
    public const string HttpDocumentsEndpoint = "/documents";
    public const string HttpIndexesEndpoint = "/indexes";
    public const string HttpDeleteDocumentEndpointWithParams = $"{HttpDocumentsEndpoint}?{WebService.IndexField}={HttpIndexPlaceholder}&{WebService.DocumentIdField}={HttpDocumentIdPlaceholder}";
    public const string HttpDeleteIndexEndpointWithParams = $"{HttpIndexesEndpoint}?{WebService.IndexField}={HttpIndexPlaceholder}";
    public const string HttpUploadStatusEndpointWithParams = $"{HttpUploadStatusEndpoint}?{WebService.IndexField}={HttpIndexPlaceholder}&{WebService.DocumentIdField}={HttpDocumentIdPlaceholder}";
    public const string HttpDownloadEndpointWithParams = $"{HttpDownloadEndpoint}?{WebService.IndexField}={HttpIndexPlaceholder}&{WebService.DocumentIdField}={HttpDocumentIdPlaceholder}&{WebService.FilenameField}={HttpFilenamePlaceholder}";
    public const string HttpIndexPlaceholder = "{index}";
    public const string HttpDocumentIdPlaceholder = "{documentId}";
    public const string HttpFilenamePlaceholder = "{filename}";

    // Pipeline Handlers, Step names
    public const string PipelineStepsExtract = "extract";
    public const string PipelineStepsPartition = "partition";
    public const string PipelineStepsGenEmbeddings = "gen_embeddings";
    public const string PipelineStepsSaveRecords = "save_records";
    public const string PipelineStepsSummarize = "summarize";
    public const string PipelineStepsDeleteGeneratedFiles = "delete_generated_files";
    public const string PipelineStepsDeleteDocument = "private_delete_document";
    public const string PipelineStepsDeleteIndex = "private_delete_index";

    // Pipeline steps
    public static readonly string[] DefaultPipeline =
    {
        PipelineStepsExtract, PipelineStepsPartition, PipelineStepsGenEmbeddings, PipelineStepsSaveRecords
    };

    public static readonly string[] PipelineWithoutSummary =
    {
        PipelineStepsExtract, PipelineStepsPartition, PipelineStepsGenEmbeddings, PipelineStepsSaveRecords
    };

    public static readonly string[] PipelineWithSummary =
    {
        PipelineStepsExtract, PipelineStepsPartition, PipelineStepsGenEmbeddings, PipelineStepsSaveRecords,
        PipelineStepsSummarize, PipelineStepsGenEmbeddings, PipelineStepsSaveRecords
    };

    public static readonly string[] PipelineOnlySummary =
    {
        PipelineStepsExtract, PipelineStepsSummarize, PipelineStepsGenEmbeddings, PipelineStepsSaveRecords
    };

    // Standard prompt names
    public const string PromptNamesSummarize = "summarize";
    public const string PromptNamesAnswerWithFacts = "answer-with-facts";
}
