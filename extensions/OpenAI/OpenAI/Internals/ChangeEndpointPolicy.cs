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
        var uri = message.Request?.Uri?.ToString()?.Replace(DefaultEndpoint, this._endpoint, StringComparison.OrdinalIgnoreCase);
        message.Request.Uri = new Uri(uri);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        var uri = message.Request?.Uri?.ToString()?.Replace(DefaultEndpoint, this._endpoint, StringComparison.OrdinalIgnoreCase);
        message.Request.Uri = new Uri(uri);
        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
    }
}
