// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticMemory.Core.Configuration;

namespace Microsoft.SemanticMemory.Core.AppBuilders;

public static class HostedHandlersBuilder
{
    private const string ConfigRoot = "SemanticMemory";

    public static WebApplicationBuilder CreateApplicationBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        var config = builder.Configuration.GetSection(ConfigRoot).Get<SemanticMemoryConfig>()
                     ?? throw new ConfigurationException("Configuration is null");
        builder.Services.ConfigureRuntime(config);

        return builder;
    }
}
