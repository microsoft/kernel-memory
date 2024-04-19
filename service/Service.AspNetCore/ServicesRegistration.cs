// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.KernelMemory.Service.AspNetCore;

public static class ServicesRegistration
{
    public static WebApplicationBuilder AddKernelMemory(this WebApplicationBuilder appBuilder, Func<IKernelMemoryBuilder, IKernelMemoryBuilder> configure)
    {
        IServiceCollection services = appBuilder.Services;
        IKernelMemoryBuilder builder = configure(new KernelMemoryBuilder(services));
        services.AddSingleton<IKernelMemory>(builder.Build());
        return appBuilder;
    }
}
