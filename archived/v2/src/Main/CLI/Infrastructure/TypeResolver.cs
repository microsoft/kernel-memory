// Copyright (c) Microsoft. All rights reserved.

using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Infrastructure;

/// <summary>
/// Adapts IServiceProvider to ITypeResolver for Spectre.Console.Cli integration.
/// Resolves command dependencies from the DI container.
/// </summary>
public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolver"/> class.
    /// </summary>
    /// <param name="provider">The service provider to wrap.</param>
    public TypeResolver(IServiceProvider provider)
    {
        this._provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// Resolves a service from the DI container.
    /// </summary>
    /// <param name="type">The service type to resolve. If null, returns null.</param>
    /// <returns>The resolved service instance, or null if type is null.</returns>
    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return this._provider.GetService(type);
    }

    /// <summary>
    /// Disposes the service provider if it implements IDisposable.
    /// </summary>
    public void Dispose()
    {
        if (this._provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
