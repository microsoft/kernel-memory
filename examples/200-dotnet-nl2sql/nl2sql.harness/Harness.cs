// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace SemanticKernel.Data.Nl2Sql.Harness;

internal static class Harness
{
    public static IConfiguration Configuration { get; } = CreateConfiguration();

    private static IConfiguration CreateConfiguration()
    {
        return
            new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .Build();
    }
}
