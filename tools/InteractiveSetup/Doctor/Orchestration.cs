// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.KernelMemory.InteractiveSetup.Doctor;

public static class Orchestration
{
    public static void Run(KernelMemoryConfig config, List<Tuple<string, string>> stats, HashSet<string> services, List<Tuple<string, string>> warnings, List<Tuple<string, string>> errors)
    {
        stats.Add("Orchestration", config.DataIngestion.OrchestrationType);
        if (string.Equals(stats.Get("Orchestration"), KernelMemoryConfig.OrchestrationTypeDistributed, StringComparison.OrdinalIgnoreCase))
        {
            stats.Add("Orchestration Service", config.DataIngestion.DistributedOrchestration.QueueType);
            services.Add(config.DataIngestion.DistributedOrchestration.QueueType);
        }
        else if (!string.IsNullOrEmpty(config.DataIngestion.DistributedOrchestration.QueueType))
        {
            warnings.Add("Orchestration Service", $"Orchestration Service is set to '{config.DataIngestion.DistributedOrchestration.QueueType}' but Orchestration is not Distributed");
        }

        // DefaultSteps
        stats.Add("Default ingestion steps", config.DataIngestion.DefaultSteps.Count == 0
            ? "system default"
            : string.Join(", ", config.DataIngestion.DefaultSteps));
    }
}
