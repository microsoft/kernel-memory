// Copyright (c) Microsoft. All rights reserved.

using System.Threading;

namespace Microsoft.KernelMemory.Context;

/// <summary>
/// Allows to store data contextual to the current asynchronous context, e.g. the current HTTP request.
/// Similar to HttpContext Items, without taking a dependency on ASP.NET Core libraries
/// </summary>
public class RequestContextProvider : IContextProvider
{
    /// <inheritdoc />
    public IContext GetContext()
    {
        return this.Context;
    }

    #region private ================================================================================

    private static readonly AsyncLocal<RequestContext> s_asyncContext = new();

    private RequestContext Context
    {
        get
        {
            if (s_asyncContext.Value == null) { s_asyncContext.Value = new RequestContext(); }

            return s_asyncContext.Value;
        }
    }

    #endregion
}
