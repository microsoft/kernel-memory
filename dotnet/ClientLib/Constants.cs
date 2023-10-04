// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory;

public static class Constants
{
    // // Default User ID owning documents uploaded without specifying a user
    // public const string DefaultDocumentOwnerUserId = "defaultUser";

    // Form field containing the User ID
    public const string WebServiceIndexField = "index";

    // Form field containing the Document ID
    public const string WebServiceDocumentIdField = "documentId";

    // Form field containing the list of pipeline steps
    public const string WebServiceStepsField = "steps";

    // Internal file used to track progress of asynchronous pipelines
    public const string PipelineStatusFilename = "__pipeline_status.json";

    // Index name used when none is specified
    public const string DefaultIndex = "default";

    // Tags reserved for internal logic
    public const char ReservedEqualsSymbol = ':';
    public const string ReservedTagsPrefix = "__";
    public const string ReservedDocumentIdTag = $"{ReservedTagsPrefix}document_id";
    public const string ReservedFileIdTag = $"{ReservedTagsPrefix}file_id";
    public const string ReservedFilePartitionTag = $"{ReservedTagsPrefix}file_part";
    public const string ReservedFileTypeTag = $"{ReservedTagsPrefix}file_type";

    // Properties stored inside the payload
    public const string ReservedPayloadTextField = "text";
    public const string ReservedPayloadFileNameField = "file";
    public const string ReservedPayloadEmbeddingSrcFileNameField = "emb_src_file";
    public const string ReservedPayloadLastUpdateField = "last_update";
    public const string ReservedPayloadVectorProviderField = "vector_provider";
    public const string ReservedPayloadVectorGeneratorField = "vector_generator";

    // Endpoints
    public const string HttpAskEndpoint = "/ask";
    public const string HttpSearchEndpoint = "/search";
    public const string HttpUploadEndpoint = "/upload";
    public const string HttpUploadStatusEndpoint = "/upload-status";
    public const string HttpDocumentsEndpoint = "/documents";
    public const string HttpDeleteEndpointWithParams = $"{HttpDocumentsEndpoint}?{WebServiceIndexField}={HttpIndexPlaceholder}&{WebServiceDocumentIdField}={HttpDocumentIdPlaceholder}";
    public const string HttpUploadStatusEndpointWithParams = $"{HttpUploadStatusEndpoint}?{WebServiceIndexField}={HttpIndexPlaceholder}&{WebServiceDocumentIdField}={HttpDocumentIdPlaceholder}";
    public const string HttpIndexPlaceholder = "{index}";
    public const string HttpDocumentIdPlaceholder = "{documentId}";

    // Handlers
    public const string DeleteDocumentPipelineStepName = "private_delete_document";

    // Pipeline steps
    public static readonly string[] DefaultPipeline = { "extract", "partition", "gen_embeddings", "save_embeddings", "summarize", "gen_embeddings", "save_embeddings" };
    public static readonly string[] PipelineWithoutSummary = { "extract", "partition", "gen_embeddings", "save_embeddings" };
}
