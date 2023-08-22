// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.ContentStorage;
using Microsoft.SemanticMemory.ContentStorage.FileSystem;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
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

public static partial class DependencyInjection
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
