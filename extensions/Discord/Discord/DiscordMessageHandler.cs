// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Sources.DiscordBot;

public sealed class DiscordMessageHandler : IPipelineStepHandler
{
    public string StepName { get; } = string.Empty;

    public DiscordMessageHandler(string stepName)
    {
        this.StepName = stepName;
    }

    public Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("## DISCORD FILE RECEIVED ##");
        return Task.FromResult((true, pipeline));
    }
}
