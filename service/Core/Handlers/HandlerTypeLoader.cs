// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

internal static class HandlerTypeLoader
{
    internal static bool TryGetHandlerType(HandlerConfig config, [NotNullWhen(true)] out Type? handlerType)
    {
        handlerType = null;

        // If part of the config is empty, the handler is disabled
        if (string.IsNullOrEmpty(config.Class) || string.IsNullOrEmpty(config.Assembly))
        {
            return false;
        }

        // Search the assembly in a few directories
        var path = string.Empty;
        var assemblyFilePaths = new HashSet<string>
        {
            config.Assembly,
            Path.Join(Environment.CurrentDirectory, config.Assembly),
            Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), config.Assembly),
        };

        foreach (var p in assemblyFilePaths)
        {
            if (!File.Exists(p)) { continue; }

            path = p;
            break;
        }

        // Check if the assembly exists
        if (string.IsNullOrEmpty(path))
        {
            throw new ConfigurationException($"Handler type loader: handler assembly not found: {config.Assembly}");
        }

        Assembly assembly = Assembly.LoadFrom(path);

        // IPipelineStepHandler
        handlerType = assembly.GetType(config.Class);

        if (!typeof(IPipelineStepHandler).IsAssignableFrom(handlerType))
        {
            throw new ConfigurationException($"Handler type loader: invalid handler definition: `{config.Class}` class doesn't implement interface {nameof(IPipelineStepHandler)}");
        }

        return true;
    }
}
