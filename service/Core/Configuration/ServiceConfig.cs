// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.KernelMemory.Configuration;

public class ServiceConfig
{
    /// <summary>
    /// Whether to run the web service that allows to upload files and search memory
    /// Use these booleans to deploy the web service and the handlers on same/different VMs
    /// </summary>
    public bool RunWebService { get; set; } = true;

    /// <summary>
    /// The maximum allowed size in bytes for a request body posted to the upload endpoint.
    /// If not set, the default ASP.NET Core limit of 30 MB (~28.6 MiB) is applied.
    /// </summary>
    public long? MaxUploadRequestBodySize { get; set; } = null;

    /// <summary>
    /// Whether to run the asynchronous pipeline handlers
    /// Use these booleans to deploy the web service and the handlers on same/different VMs
    /// </summary>
    public bool RunHandlers { get; set; } = true;

    /// <summary>
    /// Web service settings, e.g. whether to expose OpenAPI swagger docs.
    /// </summary>
    public bool OpenApiEnabled { get; set; } = false;

    /// <summary>
    /// List of handlers to enable
    /// </summary>
    public Dictionary<string, HandlerConfig> Handlers { get; set; } = new();
}
