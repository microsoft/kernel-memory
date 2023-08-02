// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.SemanticMemory.Client;

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

    /// <inheritdoc />
    public Task<string> ImportFileAsync(Document file)
    {
        return this.ImportFileInternalAsync(file);
    }

    /// <inheritdoc />
    public Task<IList<string>> ImportFilesAsync(Document[] files)
    {
        return this.ImportFilesInternalAsync(files);
    }

    /// <inheritdoc />
    public Task<string> ImportFileAsync(string fileName)
    {
        return this.ImportFileAsync(new Document(fileName));
    }

    /// <inheritdoc />
    public Task<string> ImportFileAsync(string fileName, DocumentDetails details)
    {
        return this.ImportFileInternalAsync(new Document(fileName) { Details = details });
    }

    /// <inheritdoc />
    public async Task<MemoryAnswer> AskAsync(string userId, string query)
    {
        var request = new { UserId = userId, Query = query, Tags = new TagCollection() };
        using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        HttpResponseMessage? response = await this._client.PostAsync("/ask", content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<MemoryAnswer>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new MemoryAnswer();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string userId, string documentId)
    {
        HttpResponseMessage? response = await this._client.GetAsync($"/upload-status?user={userId}&id={documentId}").ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // WORK IN PROGRESS

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // WORK IN PROGRESS

        return false;
    }

    #region private

    private async Task<IList<string>> ImportFilesInternalAsync(Document[] files)
    {
        List<string> docIds = new();
        foreach (Document file in files)
        {
            docIds.Add(await this.ImportFileInternalAsync(file).ConfigureAwait(false));
        }

        return docIds;
    }

    private async Task<string> ImportFileInternalAsync(Document file)
    {
        // Populate form with values and files from disk
        using var formData = new MultipartFormDataContent();

        using StringContent documentIdContent = new(file.Details.DocumentId);
        using (StringContent userContent = new(file.Details.UserId))
        {
            List<IDisposable> disposables = new();
            formData.Add(documentIdContent, Constants.WebServiceDocumentIdField);
            formData.Add(userContent, Constants.WebServiceUserIdField);

            foreach (var tag in file.Details.Tags.Pairs)
            {
                var tagContent = new StringContent(tag.Value);
                disposables.Add(tagContent);
                formData.Add(tagContent, tag.Key);
            }

            byte[] bytes = File.ReadAllBytes(file.FileName);
            var fileContent = new ByteArrayContent(bytes, 0, bytes.Length);
            disposables.Add(fileContent);
            formData.Add(fileContent, "file1", file.FileName);

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

        return file.Details.DocumentId;
    }

    #endregion
}
