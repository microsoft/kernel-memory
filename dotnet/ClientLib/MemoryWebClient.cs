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

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

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
    public Task<string> ImportDocumentAsync(
        Document document,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        var uploadRequest = document.ToDocumentUploadRequest(index, steps);
        return this.ImportDocumentAsync(uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        string filePath,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        var document = new Document(documentId, tags: tags).AddFile(filePath);
        var uploadRequest = document.ToDocumentUploadRequest(index, steps);
        return this.ImportDocumentAsync(uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        DocumentUploadRequest uploadRequest,
        CancellationToken cancellationToken = default)
    {
        var index = IndexExtensions.CleanName(uploadRequest.Index);
        return this.ImportInternalAsync(index, uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        Stream content,
        string? fileName = null,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        var document = new Document(documentId, tags: tags).AddStream(fileName, content);
        var uploadRequest = document.ToDocumentUploadRequest(index, steps);
        return this.ImportDocumentAsync(uploadRequest, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ImportTextAsync(
        string text,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        CancellationToken cancellationToken = default)
    {
        using Stream content = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return await this.ImportDocumentAsync(content, fileName: "content.txt", documentId: documentId, tags: tags, index: index, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsDocumentReadyAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        DataPipelineStatus? status = await this.GetDocumentStatusAsync(documentId: documentId, index: index, cancellationToken).ConfigureAwait(false);
        return status != null && status.Completed;
    }

    /// <inheritdoc />
    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        var url = Constants.HttpUploadStatusEndpointWithParams
            .Replace(Constants.HttpIndexPlaceholder, index)
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
    public async Task<SearchResult> SearchAsync(
        string query,
        string? index = null,
        MemoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        SearchQuery request = new() { Index = index, Query = query, Filter = filter ?? new MemoryFilter() };
        using StringContent content = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        HttpResponseMessage? response = await this._client.PostAsync(Constants.HttpSearchEndpoint, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<SearchResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new SearchResult();
    }

    /// <inheritdoc />
    public async Task<MemoryAnswer> AskAsync(
        string question,
        string? index = null,
        MemoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        MemoryQuery request = new() { Index = index, Question = question, Filter = filter ?? new MemoryFilter() };
        using StringContent content = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        HttpResponseMessage? response = await this._client.PostAsync(Constants.HttpAskEndpoint, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<MemoryAnswer>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new MemoryAnswer();
    }

    #region private

    private async Task<string> ImportInternalAsync(string index, DocumentUploadRequest uploadRequest, CancellationToken cancellationToken)
    {
        // Populate form with values and files from disk
        using MultipartFormDataContent formData = new();

        using StringContent indexContent = new(index);
        using (StringContent documentIdContent = new(uploadRequest.DocumentId))
        {
            List<IDisposable> disposables = new();
            formData.Add(documentIdContent, Constants.WebServiceDocumentIdField);
            formData.Add(indexContent, Constants.WebServiceIndexField);

            // Add tags to the form
            foreach (var tag in uploadRequest.Tags.Pairs)
            {
                var tagContent = new StringContent(tag.Value);
                disposables.Add(tagContent);
                formData.Add(tagContent, tag.Key);
            }

            // Add files to the form
            for (int i = 0; i < uploadRequest.Files.Count; i++)
            {
                string fileName = uploadRequest.Files[i].FileName;
                byte[] bytes;
                using (var binaryReader = new BinaryReader(uploadRequest.Files[i].FileContent))
                {
                    bytes = binaryReader.ReadBytes((int)uploadRequest.Files[i].FileContent.Length);
                }

                var fileContent = new ByteArrayContent(bytes, 0, bytes.Length);
                disposables.Add(fileContent);
                formData.Add(fileContent, $"file{i}", fileName);
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

    #endregion
}
