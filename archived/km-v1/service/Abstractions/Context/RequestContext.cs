// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.KernelMemory.Context;

public sealed class RequestContext : IContext
{
    public IDictionary<string, object?> Arguments { get; set; } = new Dictionary<string, object?>();

    public RequestContext() { }

    public RequestContext(IDictionary<string, object?>? args)
    {
        this.Arguments = args ?? new Dictionary<string, object?>();
    }
}
