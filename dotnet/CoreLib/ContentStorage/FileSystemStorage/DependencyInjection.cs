// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Core.AppBuilders;

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

    public static void AddFileSystemAsContentStorageToList(this ConfiguredServices<IContentStorage> services, FileSystemConfig config)
    {
        services.Add(serviceProvider => serviceProvider.GetService<FileSystem>()
                                        ?? throw new SemanticMemoryException("Unable to instantiate " + typeof(FileSystem)));
    }
}
