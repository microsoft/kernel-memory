// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.SemanticMemory.Core.Search;

public static partial class DependencyInjection
{
    public static IServiceCollection AddSearchClient(this IServiceCollection services)
    {
        return services.AddTransient<SearchClient, SearchClient>();
    }
}
