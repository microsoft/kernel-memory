// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.SemanticMemory.Core20;

namespace Microsoft.SemanticMemory.WebClient;

public class MemoryWebClient : ISemanticMemoryClient
{
    private readonly HttpClient _client;

    public MemoryWebClient(string endpoint) : this(endpoint, new HttpClient())
    {
    }

    public MemoryWebClient(string endpoint, HttpClient client)
    {
        this._client = client;
        this._client.BaseAddress = new Uri(endpoint);
    }

    public Task ImportFileAsync(string file, ImportFileOptions options)
    {
        return this.ImportFilesInternalAsync(new[] { file }, options);
    }

    public Task ImportFilesAsync(string[] files, ImportFileOptions options)
    {
        return this.ImportFilesInternalAsync(files, options);
    }

    public async Task<string> AskAsync(string question)
    {
        await Task.Delay(0).ConfigureAwait(false);
        return "...work in progress...";
    }

    private async Task ImportFilesInternalAsync(string[] files, ImportFileOptions options)
    {
        options.Sanitize();
        options.Validate();

        // Populate form with values and files from disk
        using var formData = new MultipartFormDataContent();

        using var requestIdContent = new StringContent(options.RequestId);
        using (var userContent = new StringContent(options.UserId))
        {
            List<IDisposable> disposables = new();
            formData.Add(requestIdContent, "requestId");
            formData.Add(userContent, "user");
            foreach (var collectionId in options.CollectionIds)
            {
                var content = new StringContent(collectionId);
                disposables.Add(content);
                formData.Add(content, "collections");
            }

            for (int index = 0; index < files.Length; index++)
            {
                string filename = files[index];
                byte[] bytes = File.ReadAllBytes(filename);
                var content = new ByteArrayContent(bytes, 0, bytes.Length);
                disposables.Add(content);
                formData.Add(content, $"file{index + 1}", filename);
            }

            // Send HTTP request
            try
            {
                HttpResponseMessage? response = await this._client.PostAsync("/upload", formData).ConfigureAwait(false);
                formData.Dispose();
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e) when (e.Data.Contains("StatusCode"))
            {
                throw new SemanticMemoryWebException($"{e.Message} [StatusCode: {e.Data["StatusCode"]}]", e);
            }
            catch (Exception e)
            {
                throw new SemanticMemoryWebException(e.Message, e);
            }
            finally
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
