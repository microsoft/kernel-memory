// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;

namespace Microsoft.KernelMemory.AI.OpenAI;

internal class ChangeEndpointPolicy : HttpPipelinePolicy
{
    internal const string DefaultEndpoint = "https://api.openai.com/v1";
    private readonly string _endpoint;

    public ChangeEndpointPolicy(string endpoint)
    {
        this._endpoint = endpoint.TrimEnd('/');
    }

    public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        var uri = message.Request.Uri.ToString().Replace(DefaultEndpoint, this._endpoint, StringComparison.OrdinalIgnoreCase);
        message.Request.Uri.Reset(new Uri(uri));
        return ProcessNextAsync(message, pipeline);
    }

    public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        ProcessNext(message, pipeline);
    }
}
