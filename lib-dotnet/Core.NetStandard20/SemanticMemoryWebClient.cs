// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.SemanticMemory.Core20;

public class SemanticMemoryWebClient : ISemanticMemoryClient
{
    private readonly HttpClient _client;

    public SemanticMemoryWebClient(string endpoint) : this(endpoint, new HttpClient())
    {
    }

    public SemanticMemoryWebClient(string endpoint, HttpClient client)
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

    private async Task ImportFilesInternalAsync(string[] files, ImportFileOptions options)
    {
        options.Sanitize();
        options.Validate();

        // Populate form with values and files from disk
        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent(options.RequestId), "requestId");
        formData.Add(new StringContent(options.UserId), "user");
        foreach (var vaultId1 in options.VaultIds)
        {
            formData.Add(new StringContent(vaultId1), "vaults");
        }

        for (int index = 0; index < files.Length; index++)
        {
            string filename = files[index];
            byte[] bytes = File.ReadAllBytes(filename);
            formData.Add(new ByteArrayContent(bytes, 0, bytes.Length), $"file{index + 1}", filename);
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
    }
}
