// Copyright (c) Microsoft. All rights reserved.

/* IMPORTANT: the Startup class must be at the root of the namespace and
 * the namespace must match exactly (required by Xunit.DependencyInjection) */

using Microsoft.Extensions.Hosting;

namespace Microsoft.AzureAISearch.UnitTests;

public class Startup
{
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
    }
}
