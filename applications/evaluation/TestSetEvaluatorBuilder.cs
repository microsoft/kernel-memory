// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Microsoft.KernelMemory.Evaluation;

public class TestSetEvaluatorBuilder
{
    // Services required to build the testset generator class
    private readonly IServiceCollection _serviceCollection;

    public TestSetEvaluatorBuilder(IServiceCollection? hostServiceCollection = null)
    {
        this._serviceCollection = new ServiceCollection();

        CopyServiceCollection(hostServiceCollection, this._serviceCollection);
    }

    public TestSetEvaluatorBuilder AddEvaluatorKernel(Kernel kernel)
    {
        this._serviceCollection.AddKeyedSingleton<Kernel>("evaluation", kernel);

        return this;
    }

    public TestSetEvaluatorBuilder WithMemory(IKernelMemory memory)
    {
        this._serviceCollection.AddSingleton<IKernelMemory>(memory);

        return this;
    }

    public TestSetEvaluator Build()
    {
        if (!this._serviceCollection.HasService<IKernelMemory>())
        {
            throw new InvalidOperationException("Memory service is required to build the TestSetEvaluator");
        }

        this._serviceCollection.AddScoped<TestSetEvaluator>(sp =>
        {
            return new TestSetEvaluator(
                sp.GetKeyedService<Kernel>("evaluation")!,
                sp.GetRequiredService<IKernelMemory>());
        });

        return this._serviceCollection.BuildServiceProvider()
            .GetRequiredService<TestSetEvaluator>();
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
