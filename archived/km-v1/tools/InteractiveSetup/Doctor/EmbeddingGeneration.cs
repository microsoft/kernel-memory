// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.KernelMemory.InteractiveSetup.Doctor;

public static class EmbeddingGeneration
{
    public static void Run(KernelMemoryConfig config, List<Tuple<string, string>> stats, HashSet<string> services, List<Tuple<string, string>> warnings, List<Tuple<string, string>> errors)
    {
        if (config.DataIngestion.EmbeddingGenerationEnabled)
        {
            stats.Add("Embedding Generation", "Enabled")
                .Add("Embedding Generators", config.DataIngestion.EmbeddingGeneratorTypes.Count == 0
                    ? "ERROR: no embedding generators configured"
                    : string.Join(", ", config.DataIngestion.EmbeddingGeneratorTypes));

            foreach (var t in config.DataIngestion.EmbeddingGeneratorTypes)
            {
                services.Add(t);
            }
        }
        else
        {
            stats.Add("Embedding Generation", "Disabled (this means your pipelines don't need vectorization, or your memory storage uses internal vectorization)");
            if (config.DataIngestion.EmbeddingGeneratorTypes.Count > 0)
            {
                stats.Add("Embedding Generators", "WARNING: some embedding generators are configured but Embedding Generation is disabled");
            }
        }
    }
}
