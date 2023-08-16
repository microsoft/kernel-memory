// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.Client;

public static class Constants
{
    // // Default User ID owning documents uploaded without specifying a user
    // public const string DefaultDocumentOwnerUserId = "defaultUser";

    // Form field containing the User ID
    public const string WebServiceIndexField = "index";

    // Form field containing the Document ID
    public const string WebServiceDocumentIdField = "documentId";

    // Internal file used to track progress of asynchronous pipelines
    public const string PipelineStatusFilename = "__pipeline_status.json";

    // Index name used when none is specified
    public const string DefaultIndex = "default";

    // Tags reserved for internal logic
    // public const string ReservedUserIdTag = "__user";
    public const string ReservedTagsPrefix = "__";
    public const string ReservedDocumentIdTag = $"{ReservedTagsPrefix}document_id";
    public const string ReservedFileIdTag = $"{ReservedTagsPrefix}file_id";
    public const string ReservedFilePartitionTag = $"{ReservedTagsPrefix}file_part";
    public const string ReservedFileTypeTag = $"{ReservedTagsPrefix}file_type";

    // Endpoints
    public const string HttpAskEndpoint = "/ask";
    public const string HttpSearchEndpoint = "/search";
    public const string HttpUploadEndpoint = "/upload";
    public const string HttpUploadStatusEndpoint = "/upload-status";
    public const string HttpUploadStatusEndpointWithParams = $"/upload-status?{WebServiceIndexField}={HttpIndexPlaceholder}&{WebServiceDocumentIdField}={HttpDocumentIdPlaceholder}";
    public const string HttpIndexPlaceholder = "{index}";
    public const string HttpDocumentIdPlaceholder = "{documentId}";
}
