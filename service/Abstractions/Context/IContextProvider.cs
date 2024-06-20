// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.KernelMemory.Context;

public interface IContextProvider
{
    /// <summary>
    /// Return the full context object.
    /// </summary>
    /// <returns>Context instance</returns>
    IContext GetContext();
}

public static class ContextProviderExtensions
{
    public static IContextProvider? InitContextArgs(this IContextProvider? provider, IDictionary<string, object?> args)
    {
        if (provider == null) { return null; }

        provider.GetContext().InitArgs(args);
        return provider;
    }

    public static IContextProvider? SetContextArgs(this IContextProvider? provider, IDictionary<string, object?> args)
    {
        if (provider == null) { return null; }

        provider.GetContext().SetArgs(args);
        return provider;
    }

    public static IContextProvider? SetContextArg(this IContextProvider? provider, string key, object? value)
    {
        if (provider == null) { return null; }

        provider.GetContext().SetArg(key, value);
        return provider;
    }
}
