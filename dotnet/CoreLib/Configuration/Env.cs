// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.SemanticMemory.Core.Configuration;

public sealed class Env
{
    public static string Var(string key)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Env>()
            .Build();

        var value = configuration[key];
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(value))
        {
            throw new ConfigurationException($"Secret / Env var not set: {key}");
        }

        return value;
    }
}
