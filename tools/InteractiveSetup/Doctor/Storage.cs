// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup.Doctor;

public static class Storage
{
    public static void Run(KernelMemoryConfig config, List<Tuple<string, string>> stats, HashSet<string> services, List<Tuple<string, string>> warnings, List<Tuple<string, string>> errors)
    {
        if (config.DataIngestion.MemoryDbTypes.Count == 0)
        {
            stats.Add("Memory DBs", "ERROR: DB(s) not configured");
            errors.Add("Memory DBs", "No memory DBs configured");
        }
        else
        {
            stats.Add("Memory DBs", string.Join(", ", config.DataIngestion.MemoryDbTypes.Select(dbType => GetServiceName(config, dbType))));
            foreach (var t in config.DataIngestion.MemoryDbTypes)
            {
                services.Add(t);
            }
        }

        if (string.IsNullOrWhiteSpace(config.DocumentStorageType))
        {
            stats.Add("Document storage", "ERROR: storage not configured");
            errors.Add("Document storage", "No document storage service configured");
        }
        else
        {
            stats.Add("Document storage", GetServiceName(config, config.DocumentStorageType));
            services.Add(config.DocumentStorageType);
        }
    }

    private static string GetServiceName(KernelMemoryConfig config, string serviceName)
    {
        return serviceName switch
        {
            "SimpleFileStorage" => $"{serviceName} {config.Services[serviceName]["StorageType"]}",
            "SimpleQueues" => $"{serviceName} {config.Services[serviceName]["StorageType"]}",
            "SimpleVectorDb" => $"{serviceName} {config.Services[serviceName]["StorageType"]}",
            _ => serviceName
        };
    }
}
