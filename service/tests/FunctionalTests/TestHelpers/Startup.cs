// Copyright (c) Microsoft. All rights reserved.

// IMPORTANT: this file must be at the root of the namespace

namespace FunctionalTests;

/// <summary>
/// IMPORTANT: this file must be at the root of the namespace
/// </summary>
public class Startup
{
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddUserSecrets<Startup>()
            .AddEnvironmentVariables()
            .Build();

        hostBuilder.ConfigureHostConfiguration(builder => builder.AddConfiguration(config));
    }
}
