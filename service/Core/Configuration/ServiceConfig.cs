﻿// Copyright (c) Microsoft. All rights reserved.

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
}
