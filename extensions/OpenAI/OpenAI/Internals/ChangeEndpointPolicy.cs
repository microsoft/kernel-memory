// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.AI.OpenAI.Internals;

internal sealed class ChangeEndpointPolicy : PipelinePolicy
{
    internal const string DefaultEndpoint = "https://api.openai.com/v1";
    private readonly string _endpoint;

    public ChangeEndpointPolicy(string endpoint)
    {
        this._endpoint = endpoint.TrimEnd('/');
    }

    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        ProcessNext(message, pipeline, currentIndex);
    }

    public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        if (message.Request.Uri != null)
        {
            var uri = message.Request.Uri.ToString().Replace(DefaultEndpoint, this._endpoint, StringComparison.OrdinalIgnoreCase);
            message.Request.Uri = new Uri(uri);
        }

        return ProcessNextAsync(message, pipeline, currentIndex);
    }
}
