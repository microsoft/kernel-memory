// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.Client;

public static class Constants
{
    // Default User ID owning documents uploaded without specifying a user
    public const string DefaultDocumentOwnerUserId = "defaultUser";

    // Form field containing the User ID
    public const string WebServiceUserIdField = "userId";

    // Form field containing the Document ID
    public const string WebServiceDocumentIdField = "documentId";

    // Internal file used to track progress of asynchronous pipelines
    public const string PipelineStatusFilename = "__pipeline_status.json";

    // Tags reserved for internal logic
    public const string ReservedUserIdTag = "__user";
    public const string ReservedPipelineIdTag = "__pipeline_id";
    public const string ReservedFileIdTag = "__file_id";
    public const string ReservedFilePartitionTag = "__file_part";
    public const string ReservedFileTypeTag = "__file_type";

    // Endpoints
    public const string HttpAskEndpoint = "/ask";
    public const string HttpSearchEndpoint = "/search";
    public const string HttpUploadEndpoint = "/upload";
    public const string HttpUploadStatusEndpoint = "/upload-status";
    public const string HttpUploadStatusEndpointWithParams = "/upload-status?user={userId}&id={documentId}";
    public const string HttpUserIdPlaceholder = "{userId}";
    public const string HttpDocumentIdPlaceholder = "{documentId}";
}
