// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.Configuration;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public sealed class Env
{
    public static string Var(string key)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly())
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
