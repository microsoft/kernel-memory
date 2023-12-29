// Copyright (c) Microsoft. All rights reserved.
//
// /* IMPORTANT: the Startup class must be at the root of the namespace and
// * the namespace must match exactly (required by Xunit.DependencyInjection) */
//
// // ReSharper disable CheckNamespace

using Microsoft.Extensions.Hosting;

namespace UnitTests;

public class Startup
{
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
    }
}
