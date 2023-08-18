// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.ContentStorage.FileSystem;

public static class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithFilesystemStorage(this MemoryClientBuilder builder, FileSystemConfig config)
    {
        builder.Services.AddFileSystemAsContentStorage(config);
        return builder;
    }

    public static MemoryClientBuilder WithFilesystemStorage(this MemoryClientBuilder builder, string directory)
    {
        builder.Services.AddFileSystemAsContentStorage(directory);
        return builder;
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddFileSystemAsContentStorage(this IServiceCollection services, FileSystemConfig config)
    {
        return services
            .AddSingleton<FileSystemConfig>(config)
            .AddSingleton<IContentStorage, FileSystemStorage>();
    }

    public static IServiceCollection AddFileSystemAsContentStorage(this IServiceCollection services, string directory)
    {
        var config = new FileSystemConfig { Directory = directory };
        return services.AddFileSystemAsContentStorage(config);
    }
}
