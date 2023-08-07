// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.ContentStorage.FileSystemStorage;

public static partial class DependencyInjection
{
    public static IServiceCollection AddFileSystemAsContentStorage(this IServiceCollection services, FileSystemConfig config)
    {
        return services
            .AddSingleton<IContentStorage>(serviceProvider => new FileSystem(
                directory: config.Directory, logger: serviceProvider.GetService<ILogger<FileSystem>>()))
            .AddSingleton<FileSystem>(serviceProvider => new FileSystem(
                directory: config.Directory, logger: serviceProvider.GetService<ILogger<FileSystem>>()));
    }

    public static void AddFileSystemAsContentStorageToList(this ConfiguredServices<IContentStorage> services, FileSystemConfig config)
    {
        services.Add(serviceProvider => new FileSystem(directory: config.Directory, logger: serviceProvider.GetService<ILogger<FileSystem>>()));
    }
}
