// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticMemory.Client.Models;

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
    public Task<string> ImportFileAsync(Document file, CancellationToken cancellationToken = default)
    {
        return this.ImportFileInternalAsync(file, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IList<string>> ImportFilesAsync(Document[] files, CancellationToken cancellationToken = default)
    {
        return this.ImportFilesInternalAsync(files, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return this.ImportFileAsync(new Document(fileName), cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportFileAsync(string fileName, DocumentDetails details, CancellationToken cancellationToken = default)
    {
        return this.ImportFileInternalAsync(new Document(fileName) { Details = details }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(string query, CancellationToken cancellationToken = default)
    {
        return this.AskAsync(new DocumentDetails().UserId, query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MemoryAnswer> AskAsync(string userId, string query, CancellationToken cancellationToken = default)
    {
        var request = new { UserId = userId, Query = query, Tags = new TagCollection() };
        using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        HttpResponseMessage? response = await this._client.PostAsync("/ask", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<MemoryAnswer>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new MemoryAnswer();
    }

    /// <inheritdoc />
    public async Task<bool> IsReadyAsync(string userId, string documentId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage? response = await this._client.GetAsync($"/upload-status?user={userId}&id={documentId}", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        DataPipelineStatus? status = JsonSerializer.Deserialize<DataPipelineStatus>(json);

        if (status == null)
        {
            throw new SemanticMemoryWebException("Unable to parse status response");
        }

        return status.Completed;
    }

    #region private

    private async Task<IList<string>> ImportFilesInternalAsync(Document[] files, CancellationToken cancellationToken)
    {
        List<string> docIds = new();
        foreach (Document file in files)
        {
            docIds.Add(await this.ImportFileInternalAsync(file, cancellationToken).ConfigureAwait(false));
        }

        return docIds;
    }

    private async Task<string> ImportFileInternalAsync(Document file, CancellationToken cancellationToken)
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
                HttpResponseMessage? response = await this._client.PostAsync("/upload", formData, cancellationToken).ConfigureAwait(false);
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
