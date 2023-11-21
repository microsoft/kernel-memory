// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Search;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class DependencyInjection
{
    public static IServiceCollection AddSearchClient(this IServiceCollection services)
    {
        return services.AddTransient<SearchClient, SearchClient>();
    }
}
