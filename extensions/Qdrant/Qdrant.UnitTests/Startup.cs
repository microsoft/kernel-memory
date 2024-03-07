// Copyright (c) Microsoft. All rights reserved.

/* IMPORTANT: the Startup class must be at the root of the namespace and
 * the namespace must match exactly (required by Xunit.DependencyInjection) */

using Microsoft.Extensions.Hosting;

namespace Qdrant.UnitTests;

public class Startup
{
    // ReSharper disable once UnusedMember.Global
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
    }
}
