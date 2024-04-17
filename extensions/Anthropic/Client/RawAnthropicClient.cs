// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Microsoft.KernelMemory.AI.Anthropic.Client;

internal sealed class RawAnthropicClient
{
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _endpointVersion;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _httpClientName;

    internal RawAnthropicClient(
        string apiKey,
        string endpoint,
        string endpointVersion,
        IHttpClientFactory httpClientFactory,
        string? httpClientName)
    {
        this._apiKey = apiKey;
        this._endpoint = endpoint.TrimEnd('/');
        this._endpointVersion = endpointVersion;
        this._httpClientFactory = httpClientFactory;
        this._httpClientName = httpClientName;
    }

    internal async IAsyncEnumerable<StreamingResponseMessage> CallClaudeStreamingAsync(CallClaudeStreamingParams parameters)
    {
        var requestPayload = new MessageRequest
        {
            Model = parameters.ModelName,
            MaxTokens = parameters.MaxTokens,
            Temperature = parameters.Temperature,
            System = parameters.System ?? "You are an helpful assistant.",
            Stream = true,
            Messages = new[]
            {
                new Message
                {
                    Role = "user",
                    Content = parameters.Prompt
                }
            }
        };

        string jsonPayload = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        content.Headers.Add("x-api-key", this._apiKey);
        content.Headers.Add("anthropic-version", this._endpointVersion);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{this._endpoint}/v1/messages")
        {
            Content = content,
        };

#pragma warning disable CA2000 // Dispose objects before losing scope
        var httpClient = this.GetHttpClient();
#pragma warning restore CA2000 // Dispose objects before losing scope

        var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var responseError = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new KernelMemoryException($"Failed to send request: {response.StatusCode} - {responseError}");
        }

        response.EnsureSuccessStatusCode();
        var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        using (StreamReader reader = new(responseStream))
        {
            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (line == null)
                {
                    break; //end of stream
                }

                //this is the first line of message
                var eventMessage = line.Split(":")[1].Trim();

                //now read the message
                line = await reader.ReadLineAsync().ConfigureAwait(false)!;

                if (line == null)
                {
                    break; //end of stream
                }

                if (eventMessage == "content_block_delta")
                {
                    var data = line.Substring("data: ".Length).Trim();
                    var messageDelta = JsonSerializer.Deserialize<ContentBlockDelta>(data);
                    yield return messageDelta!;
                }
                else if (eventMessage == "message_stop")
                {
                    break;
                }

                //read the next empty line
                await reader.ReadLineAsync().ConfigureAwait(false);
            }
        }
    }

    private HttpClient GetHttpClient()
    {
        if (String.IsNullOrEmpty(this._httpClientName))
        {
            return this._httpClientFactory.CreateClient();
        }

        return this._httpClientFactory.CreateClient(this._httpClientName);
    }
}
