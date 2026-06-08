// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace KernelMemory.Main.CLI.Infrastructure;

/// <summary>
/// Adapts IServiceCollection to ITypeRegistrar for Spectre.Console.Cli integration.
/// Enables dependency injection in CLI commands.
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="services">The service collection to wrap.</param>
    public TypeRegistrar(IServiceCollection services)
    {
        this._services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Registers a service with its implementation.
    /// </summary>
    /// <param name="service">The service type.</param>
    /// <param name="implementation">The implementation type.</param>
    public void Register(Type service, Type implementation)
    {
        this._services.AddSingleton(service, implementation);
    }

    /// <summary>
    /// Registers a service instance.
    /// </summary>
    /// <param name="service">The service type.</param>
    /// <param name="implementation">The service instance.</param>
    public void RegisterInstance(Type service, object implementation)
    {
        this._services.AddSingleton(service, implementation);
    }

    /// <summary>
    /// Registers a service with a factory function.
    /// </summary>
    /// <param name="service">The service type.</param>
    /// <param name="factory">The factory function.</param>
    public void RegisterLazy(Type service, Func<object> factory)
    {
        this._services.AddSingleton(service, _ => factory());
    }

    /// <summary>
    /// Builds the service provider and returns a type resolver.
    /// </summary>
    /// <returns>A type resolver wrapping the service provider.</returns>
    public ITypeResolver Build()
    {
        return new TypeResolver(this._services.BuildServiceProvider());
    }
}
