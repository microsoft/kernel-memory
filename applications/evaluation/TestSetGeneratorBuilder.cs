// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.SemanticKernel;

namespace Microsoft.KernelMemory.Evaluation;

public sealed class TestSetGeneratorBuilder
{
    // Services required to build the testset generator class
    private readonly IServiceCollection _serviceCollection;

    public TestSetGeneratorBuilder(IServiceCollection? hostServiceCollection = null)
    {
        this._serviceCollection = new ServiceCollection();

        CopyServiceCollection(hostServiceCollection, this._serviceCollection);
    }

    public TestSetGeneratorBuilder AddIngestionMemoryDb(IMemoryDb service)
    {
        this._serviceCollection.AddSingleton<IMemoryDb>(service);

        return this;
    }

    public TestSetGeneratorBuilder AddEvaluatorKernel(Kernel kernel)
    {
        this._serviceCollection.AddKeyedSingleton<Kernel>("evaluation", kernel);

        return this;
    }

    public TestSetGeneratorBuilder AddTranslatorKernel(Kernel kernel)
    {
        this._serviceCollection.AddKeyedSingleton<Kernel>("translation", kernel);

        return this;
    }

    public TestSetGenerator Build()
    {
        if (!this._serviceCollection.HasService<IMemoryDb>())
        {
            throw new InvalidOperationException("MemoryDb service is required to build the TestSetGenerator");
        }

        this._serviceCollection.AddScoped<TestSetGenerator>(sp =>
        {
            return new TestSetGenerator(
                sp.GetRequiredKeyedService<Kernel>("evaluation"),
                sp.GetKeyedService<Kernel>("translation"),
                sp.GetRequiredService<IMemoryDb>());
        });

        return this._serviceCollection.BuildServiceProvider()
            .GetRequiredService<TestSetGenerator>();
    }

    private static void CopyServiceCollection(
        IServiceCollection? source,
        IServiceCollection destination)
    {
        if (source == null) { return; }

        foreach (ServiceDescriptor d in source)
        {
            destination.Add(d);
        }
    }
}
