// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ClientModel.Primitives;

namespace Microsoft.KernelMemory.AI.OpenAI;

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
        ArgumentNullException.ThrowIfNull(message);
        if (message.Request == null)
        {
            throw new ArgumentNullException(nameof(message.Request));
        }

        if (message.Request.Uri == null)
        {
            throw new ArgumentNullException(nameof(message.Request.Uri));
        }

        var uri = message.Request.Uri.ToString()?.Replace(DefaultEndpoint, this._endpoint, StringComparison.OrdinalIgnoreCase);
        if (uri == null)
        {
            throw new InvalidOperationException("URI replacement resulted in a null value.");
        }

        message.Request.Uri = new Uri(uri);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.Request == null)
        {
            throw new ArgumentNullException(nameof(message.Request));
        }

        if (message.Request.Uri == null)
        {
            throw new ArgumentNullException(nameof(message.Request.Uri));
        }

        var uri = message.Request.Uri.ToString()?.Replace(DefaultEndpoint, this._endpoint, StringComparison.OrdinalIgnoreCase);
        if (uri == null)
        {
            throw new InvalidOperationException("URI replacement resulted in a null value.");
        }

        message.Request.Uri = new Uri(uri);
        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
    }
}
