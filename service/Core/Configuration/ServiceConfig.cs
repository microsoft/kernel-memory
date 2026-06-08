// Copyright (c) Microsoft. All rights reserved.

using System;
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
    /// Whether to run the asynchronous pipeline handlers
    /// Use these booleans to deploy the web service and the handlers on same/different VMs
    /// </summary>
    public bool RunHandlers { get; set; } = true;

    /// <summary>
    /// Web service settings, e.g. whether to expose OpenAPI swagger docs.
    /// </summary>
    public bool OpenApiEnabled { get; set; } = false;

    /// <summary>
    /// Whether to send a [DONE] message at the end of SSE streams.
    /// </summary>
    public bool SendSSEDoneMessage { get; set; } = true;

    /// <summary>
    /// List of handlers to enable
    /// </summary>
    public Dictionary<string, HandlerConfig> Handlers { get; set; } = [];

    /// <summary>
    /// The maximum allowed size in megabytes for an HTTP request body posted to the upload endpoint.
    /// If not set the solution defaults to 30,000,000 bytes (~28.6 MB) (ASP.NET default).
    /// Note: this applies only to KM HTTP service.
    /// </summary>
    public long? MaxUploadSizeMb { get; set; } = null;
}

public static partial class ServiceConfigExtensions
{
    public static long? GetMaxUploadSizeInBytes(this ServiceConfig config)
    {
        if (config.MaxUploadSizeMb.HasValue)
        {
            return Math.Max(1, config.MaxUploadSizeMb.Value) * 1024 * 1024;
        }

        return null;
    }
}
