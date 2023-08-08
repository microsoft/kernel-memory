// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.SemanticMemory.Core.ContentStorage.FileSystemStorage;

public static partial class DependencyInjection
{
    public static IServiceCollection AddFileSystemAsContentStorage(this IServiceCollection services, FileSystemConfig config)
    {
        return services
            .AddSingleton<FileSystemConfig>(config)
            .AddSingleton<IContentStorage, FileSystem>()
            .AddSingleton<FileSystem, FileSystem>();
    }
}
