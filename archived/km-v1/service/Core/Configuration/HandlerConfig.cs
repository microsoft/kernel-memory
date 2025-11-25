// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.Configuration;

public class HandlerConfig
{
    /// <summary>
    /// .NET assembly containing the handler class
    /// </summary>
    public string Assembly { get; set; }

    /// <summary>
    /// .NET class in the assembly, containing the handler logic
    /// </summary>
    public string Class { get; set; }

    public HandlerConfig()
    {
        this.Assembly = string.Empty;
        this.Class = string.Empty;
    }

    public HandlerConfig(string assembly, string className)
    {
        this.Assembly = assembly;
        this.Class = className;
    }
}
