// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup.Doctor;

public static class Check
{
    public static void Run()
    {
        KernelMemoryConfig? config = AppSettings.ReadConfig();

        if (config == null)
        {
            Console.WriteLine("No configuration found.");
            return;
        }

        var stats = new List<Tuple<string, string>>();
        var services = new HashSet<string>();
        var warnings = new List<Tuple<string, string>>();
        var errors = new List<Tuple<string, string>>();

        Orchestration.Run(config, stats, services, warnings, errors);
        stats.AddSeparator();
        EmbeddingGeneration.Run(config, stats, services, warnings, errors);
        Storage.Run(config, stats, services, warnings, errors);
        stats.AddSeparator();

        // Partitioning
        stats.Add("Text partitioning", $"Chunk:{config.DataIngestion.TextPartitioning.MaxTokensPerParagraph}; Overlapping:{config.DataIngestion.TextPartitioning.OverlappingTokens}");

        // Image OCR
        stats.Add("Image OCR", string.IsNullOrWhiteSpace(config.DataIngestion.ImageOcrType) ? "Disabled" : config.DataIngestion.ImageOcrType);

        // Text Generation
        if (string.IsNullOrWhiteSpace(config.TextGeneratorType))
        {
            errors.Add("Text Generation", "No text generation service configured");
        }
        else
        {
            stats.Add("Text Generation", config.TextGeneratorType);
        }

        // Moderation
        stats.Add("Moderation", string.IsNullOrWhiteSpace(config.ContentModerationType) ? "Disabled" : config.ContentModerationType);
        if (string.IsNullOrWhiteSpace(config.ContentModerationType))
        {
            warnings.Add("Moderation", "No moderation service configured");
        }

        ShowStats("Service configuration", stats);
        Services.CheckAndShow(config, services, warnings, errors);
        ShowStats("Warnings", warnings);
        ShowStats("Errors", errors);
        Environment.Exit(0);
    }

    private static void ShowStats(string title, List<Tuple<string, string>> stats)
    {
        var columnWidth = 1 + stats.Select(stat => stat.Item1.Length).Prepend(0).Max();

        Console.WriteLine($"\n\u001b[1;37m### {title}\u001b[0m\n");
        var count = 0;
        foreach (var kv in stats)
        {
            if (string.IsNullOrWhiteSpace(kv.Item1))
            {
                Console.WriteLine();
                continue;
            }

            Console.WriteLine($"{kv.Item1.PadRight(columnWidth)}: {kv.Item2}");
            count++;
        }

        if (count == 0)
        {
            Console.WriteLine("None.");
        }
    }
}
