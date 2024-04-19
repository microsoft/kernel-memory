// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.KernelMemory.Service.AspNetCore;

public static class ServicesRegistration
{
    public static WebApplicationBuilder AddKernelMemory(
        this WebApplicationBuilder appBuilder, Func<IKernelMemoryBuilder, IKernelMemoryBuilder> configure)
    {
        IKernelMemoryBuilder builder = configure(new KernelMemoryBuilder(appBuilder.Services));
        appBuilder.Services.AddSingleton<IKernelMemory>(builder.Build());
        return appBuilder;
    }
}
