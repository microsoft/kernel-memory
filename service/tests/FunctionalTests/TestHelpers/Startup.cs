// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // Must be in the root of the namespace
// ReSharper disable once CheckNamespace
namespace FunctionalTests;

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
