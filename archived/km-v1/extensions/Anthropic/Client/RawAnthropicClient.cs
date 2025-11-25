// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI.Anthropic.Client;

internal sealed class RawAnthropicClient
{
    private const string ApiKeyHeader = "x-api-key";
    private const string EndpointVersionHeader = "anthropic-version";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _endpointVersion;

    internal RawAnthropicClient(HttpClient httpClient, string endpoint, string endpointVersion, string apiKey)
    {
        this._httpClient = httpClient;
        this._httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Telemetry.HttpUserAgent);
        this._endpoint = endpoint.TrimEnd('/');
        this._endpointVersion = endpointVersion;
        this._apiKey = apiKey;
    }

    internal async IAsyncEnumerable<StreamingResponseMessage> CallClaudeStreamingAsync(
        CallClaudeStreamingParams parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requestPayload = new MessageRequest
        {
            Model = parameters.ModelName,
            MaxTokens = parameters.MaxTokens,
            Temperature = parameters.Temperature,
            System = parameters.System,
            Stream = true,
            Messages =
            [
                new Message
                {
                    Role = "user",
                    Content = parameters.Prompt
                }
            ]
        };

        string jsonPayload = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.Add(ApiKeyHeader, this._apiKey);
        content.Headers.Add(EndpointVersionHeader, this._endpointVersion);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{this._endpoint}/v1/messages");
        request.Content = content;

        var response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var responseError = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new KernelMemoryException($"Failed to send request: {response.StatusCode} - {responseError}",
                isTransient: response.StatusCode.IsTransientError());
        }

        var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        using StreamReader reader = new(responseStream);
        while (!reader.EndOfStream)
        {
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (line == null)
            {
                break; //end of stream
            }

            //this is the first line of message
            var eventMessage = line.Split(":")[1].Trim();

            //now read the message
            line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (line == null)
            {
                break; //end of stream
            }

            if (eventMessage == "content_block_delta")
            {
                string data = line.Substring("data: ".Length).Trim();
                ContentBlockDelta? messageDelta = JsonSerializer.Deserialize<ContentBlockDelta>(data);
                if (messageDelta == null)
                {
                    // TODO: log error, throw exception?
                    continue;
                }

                yield return messageDelta;
            }
            else if (eventMessage == "message_stop")
            {
                break;
            }

            // Read the next empty line
            await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
