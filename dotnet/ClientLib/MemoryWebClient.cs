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
    public Task<string> ImportDocumentAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken = default)
    {
        return this.ImportInternalAsync(uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        return this.ImportInternalAsync(document, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(string fileName, DocumentDetails? details = null, CancellationToken cancellationToken = default)
    {
        return this.ImportInternalAsync(new Document(fileName) { Details = details ?? new DocumentDetails() }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsDocumentReadyAsync(string userId, string documentId, CancellationToken cancellationToken = default)
    {
        DataPipelineStatus? status = await this.GetDocumentStatusAsync(userId: userId, documentId: documentId, cancellationToken).ConfigureAwait(false);
        return status != null && status.Completed;
    }

    /// <inheritdoc />
    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(string userId, string documentId, CancellationToken cancellationToken = default)
    {
        var url = Constants.HttpUploadStatusEndpointWithParams
            .Replace(Constants.HttpUserIdPlaceholder, userId)
            .Replace(Constants.HttpDocumentIdPlaceholder, documentId);
        HttpResponseMessage? response = await this._client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        DataPipelineStatus? status = JsonSerializer.Deserialize<DataPipelineStatus>(json);

        if (status == null)
        {
            return null;
        }

        return status;
    }

    /// <inheritdoc />
    public Task<SearchResult> SearchAsync(string query, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        // TODO: the user ID might be in the filter
        return this.SearchAsync(new DocumentDetails().UserId, query, filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(string userId, string query, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        SearchQuery request = new() { UserId = userId, Query = query, Filter = filter ?? new MemoryFilter() };
        using StringContent content = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        HttpResponseMessage? response = await this._client.PostAsync(Constants.HttpSearchEndpoint, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<SearchResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new SearchResult();
    }

    /// <inheritdoc />
    public Task<MemoryAnswer> AskAsync(string question, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return this.AskAsync(new DocumentDetails().UserId, question, filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MemoryAnswer> AskAsync(string userId, string question, MemoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        MemoryQuery request = new() { UserId = userId, Question = question, Filter = filter ?? new MemoryFilter() };
        using StringContent content = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        HttpResponseMessage? response = await this._client.PostAsync(Constants.HttpAskEndpoint, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<MemoryAnswer>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new MemoryAnswer();
    }

    #region private

    private async Task<string> ImportInternalAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken)
    {
        // Populate form with values and files from disk
        using MultipartFormDataContent formData = new();

        using StringContent documentIdContent = new(uploadRequest.DocumentId);
        using (StringContent userContent = new(uploadRequest.UserId))
        {
            List<IDisposable> disposables = new();
            formData.Add(documentIdContent, Constants.WebServiceDocumentIdField);
            formData.Add(userContent, Constants.WebServiceUserIdField);

            foreach (var tag in uploadRequest.Tags.Pairs)
            {
                var tagContent = new StringContent(tag.Value);
                disposables.Add(tagContent);
                formData.Add(tagContent, tag.Key);
            }

            for (int index = 0; index < uploadRequest.Files.Count; index++)
            {
                string fileName = uploadRequest.Files[index].FileName;

                byte[] bytes;
                using (var binaryReader = new BinaryReader(uploadRequest.Files[index].FileContent))
                {
                    bytes = binaryReader.ReadBytes((int)uploadRequest.Files[index].FileContent.Length);
                }

                var fileContent = new ByteArrayContent(bytes, 0, bytes.Length);
                disposables.Add(fileContent);

                formData.Add(fileContent, $"file{index}", fileName);
            }

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

        return uploadRequest.DocumentId;
    }

    private async Task<string> ImportInternalAsync(Document document, CancellationToken cancellationToken)
    {
        // Populate form with values and files from disk
        using MultipartFormDataContent formData = new();

        using StringContent documentIdContent = new(document.Details.DocumentId);
        using (StringContent userContent = new(document.Details.UserId))
        {
            List<IDisposable> disposables = new();
            formData.Add(documentIdContent, Constants.WebServiceDocumentIdField);
            formData.Add(userContent, Constants.WebServiceUserIdField);

            foreach (var tag in document.Details.Tags.Pairs)
            {
                var tagContent = new StringContent(tag.Value);
                disposables.Add(tagContent);
                formData.Add(tagContent, tag.Key);
            }

            for (int index = 0; index < document.FileNames.Count; index++)
            {
                string fileName = document.FileNames[index];
                byte[] bytes = File.ReadAllBytes(fileName);
                var fileContent = new ByteArrayContent(bytes, 0, bytes.Length);
                disposables.Add(fileContent);
                formData.Add(fileContent, $"file{index}", fileName);
            }

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

        return document.Details.DocumentId;
    }

    #endregion
}
