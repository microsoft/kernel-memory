// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.Context;
using Microsoft.KernelMemory.Internals;

namespace Microsoft.KernelMemory;

#pragma warning disable CA2234 // using string URIs is ok

/// <summary>
/// Kernel Memory web service client
/// </summary>
public sealed class MemoryWebClient : IKernelMemory
{
    private static readonly JsonSerializerOptions s_caseInsensitiveJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _client;

    /// <summary>
    /// New instance of web client to use Kernel Memory web service
    /// </summary>
    /// <param name="endpoint">Kernel Memory web service endpoint</param>
    /// <param name="apiKey">Kernel Memory web service API Key (if configured)</param>
    /// <param name="apiKeyHeader">Name of HTTP header to use to send API Key</param>
    public MemoryWebClient(string endpoint, string? apiKey = "", string apiKeyHeader = "Authorization")
        : this(endpoint, new HttpClient(), apiKey: apiKey, apiKeyHeader: apiKeyHeader)
    {
    }

    /// <summary>
    /// New instance of web client to use Kernel Memory web service
    /// </summary>
    /// <param name="endpoint">Kernel Memory web service endpoint</param>
    /// <param name="client">Custom HTTP Client to use (note: BaseAddress is overwritten)</param>
    /// <param name="apiKey">Kernel Memory web service API Key (if configured)</param>
    /// <param name="apiKeyHeader">Name of HTTP header to use to send API Key</param>
    public MemoryWebClient(string endpoint, HttpClient client, string? apiKey = "", string apiKeyHeader = "Authorization")
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint), "Kernel Memory endpoint is empty");

        this._client = client;
        this._client.BaseAddress = new Uri(endpoint.CleanBaseAddress());

        if (!string.IsNullOrEmpty(apiKey))
        {
            if (string.IsNullOrEmpty(apiKeyHeader))
            {
                throw new KernelMemoryException("The name of the HTTP header to pass the API Key is empty");
            }

            this._client.DefaultRequestHeaders.Add(apiKeyHeader, apiKey);
        }
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        Document document,
        string? index = null,
        IEnumerable<string>? steps = null,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        DocumentUploadRequest uploadRequest = new(document, index, steps);
        return this.ImportDocumentAsync(uploadRequest, context, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        string filePath,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var document = new Document(documentId, tags: tags).AddFile(filePath);
        DocumentUploadRequest uploadRequest = new(document, index, steps);
        return this.ImportDocumentAsync(uploadRequest, context, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        DocumentUploadRequest uploadRequest,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        return this.ImportInternalAsync(uploadRequest.Index, uploadRequest, context, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ImportDocumentAsync(
        Stream content,
        string? fileName = null,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var document = new Document(documentId, tags).AddStream(fileName, content);
        DocumentUploadRequest uploadRequest = new(document, index, steps);
        return this.ImportDocumentAsync(uploadRequest, context, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ImportTextAsync(
        string text,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        Stream content = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await using (content.ConfigureAwait(false))
        {
            return await this.ImportDocumentAsync(
                    content: content,
                    fileName: "content.txt",
                    documentId: documentId,
                    tags: tags,
                    index: index,
                    steps: steps,
                    context: context,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<string> ImportWebPageAsync(
        string url,
        string? documentId = null,
        TagCollection? tags = null,
        string? index = null,
        IEnumerable<string>? steps = null,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var uri = new Uri(url);
        Verify.ValidateUrl(uri.AbsoluteUri, requireHttps: false, allowReservedIp: false, allowQuery: true);

        Stream content = new MemoryStream(Encoding.UTF8.GetBytes(uri.AbsoluteUri));
        await using (content.ConfigureAwait(false))
        {
            return await this.ImportDocumentAsync(
                    content: content,
                    fileName: "content.url",
                    documentId: documentId,
                    tags: tags,
                    index: index,
                    steps: steps,
                    context: context,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IndexDetails>> ListIndexesAsync(CancellationToken cancellationToken = default)
    {
        var url = Constants.HttpIndexesEndpoint.CleanUrlPath();
        HttpResponseMessage response = await this._client.GetAsync(url, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var data = JsonSerializer.Deserialize<IndexCollection>(json, s_caseInsensitiveJsonOptions) ?? new IndexCollection();

        return data.Results;
    }

    /// <inheritdoc />
    public async Task DeleteIndexAsync(string? index = null, CancellationToken cancellationToken = default)
    {
        var url = Constants.HttpDeleteIndexEndpointWithParams
            .Replace(Constants.HttpIndexPlaceholder, index, StringComparison.OrdinalIgnoreCase)
            .CleanUrlPath();
        HttpResponseMessage response = await this._client.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

        // No error if the index doesn't exist
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            throw new KernelMemoryException($"Index delete failed, status code: {response.StatusCode}", e);
        }
    }

    /// <inheritdoc />
    public async Task DeleteDocumentAsync(string documentId, string? index = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new KernelMemoryException("The document ID is empty");
        }

        var url = Constants.HttpDeleteDocumentEndpointWithParams
            .Replace(Constants.HttpIndexPlaceholder, index, StringComparison.OrdinalIgnoreCase)
            .Replace(Constants.HttpDocumentIdPlaceholder, documentId, StringComparison.OrdinalIgnoreCase)
            .CleanUrlPath();
        HttpResponseMessage response = await this._client.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

        // No error if the document doesn't exist
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            throw new KernelMemoryException($"Document deletion failed, status code: {response.StatusCode}", e);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsDocumentReadyAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        DataPipelineStatus? status = await this.GetDocumentStatusAsync(documentId: documentId, index: index, cancellationToken).ConfigureAwait(false);
        return status != null && status.Completed && !status.Empty;
    }

    /// <inheritdoc />
    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        var url = Constants.HttpUploadStatusEndpointWithParams
            .Replace(Constants.HttpIndexPlaceholder, index, StringComparison.OrdinalIgnoreCase)
            .Replace(Constants.HttpDocumentIdPlaceholder, documentId, StringComparison.OrdinalIgnoreCase)
            .CleanUrlPath();
        HttpResponseMessage response = await this._client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        DataPipelineStatus? status = JsonSerializer.Deserialize<DataPipelineStatus>(json);

        return status;
    }

    /// <inheritdoc />
    public async Task<StreamableFileContent> ExportFileAsync(
        string documentId,
        string fileName,
        string? index = null,
        CancellationToken cancellationToken = default)
    {
        var url = Constants.HttpDownloadEndpointWithParams
            .Replace(Constants.HttpIndexPlaceholder, index, StringComparison.OrdinalIgnoreCase)
            .Replace(Constants.HttpDocumentIdPlaceholder, documentId, StringComparison.OrdinalIgnoreCase)
            .Replace(Constants.HttpFilenamePlaceholder, fileName, StringComparison.OrdinalIgnoreCase)
            .CleanUrlPath();

        HttpResponseMessage httpResponse = await this._client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        ArgumentNullExceptionEx.ThrowIfNull(httpResponse, nameof(httpResponse), "KernelMemory HTTP response is NULL");

        httpResponse.EnsureSuccessStatusCode();
        (string contentType, long contentLength, DateTimeOffset lastModified) = GetFileDetails(httpResponse);

        return new StreamableFileContent(
            fileName: fileName,
            fileSize: contentLength,
            fileType: contentType,
            lastWriteTimeUtc: lastModified,
            asyncStreamDelegate: httpResponse.Content.ReadAsStreamAsync);
    }

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(
        string query,
        string? index = null,
        MemoryFilter? filter = null,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = -1,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (filter != null)
        {
            if (filters == null) { filters = new List<MemoryFilter>(); }

            filters.Add(filter);
        }

        SearchQuery request = new()
        {
            Index = index,
            Query = query,
            Filters = (filters is { Count: > 0 }) ? filters.ToList() : new(),
            MinRelevance = minRelevance,
            Limit = limit,
            ContextArguments = (context?.Arguments ?? new Dictionary<string, object?>()).ToDictionary(),
        };
        using StringContent content = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var url = Constants.HttpSearchEndpoint.CleanUrlPath();
        HttpResponseMessage response = await this._client.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SearchResult>(json, s_caseInsensitiveJsonOptions) ?? new SearchResult();
    }

    /// <inheritdoc />
    public async Task<MemoryAnswer> AskAsync(
        string question,
        string? index = null,
        MemoryFilter? filter = null,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (filter != null)
        {
            if (filters == null) { filters = new List<MemoryFilter>(); }

            filters.Add(filter);
        }

        MemoryQuery request = new()
        {
            Index = index,
            Question = question,
            Filters = (filters is { Count: > 0 }) ? filters.ToList() : new(),
            MinRelevance = minRelevance,
            ContextArguments = (context?.Arguments ?? new Dictionary<string, object?>()).ToDictionary(),
        };
        using StringContent content = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var url = Constants.HttpAskEndpoint.CleanUrlPath();
        HttpResponseMessage response = await this._client.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<MemoryAnswer>(json, s_caseInsensitiveJsonOptions) ?? new MemoryAnswer();
    }

    #region private

    private static (string contentType, long contentLength, DateTimeOffset lastModified) GetFileDetails(HttpResponseMessage response)
    {
        string contentType = "application/octet-stream";
        long contentLength = 0;
        DateTimeOffset lastModified = DateTimeOffset.MinValue;

        // Headers example:
        // - HTTP/1.1 200 OK
        // - Content-Length: 96195
        // - Content-Type: text/plain
        // - Date: Fri, 13 May 2044 10:09:30 GMT
        // - Server: Kestrel
        // - Accept-Ranges: bytes
        // - Last-Modified: Tue, 03 May 2044 09:10:30 GMT
        // - Content-Disposition: attachment; filename=file1.pdf; filename*=UTF-8''file1.pdf
        response.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string>? contentTypeValues);
        response.Content.Headers.TryGetValues("Content-Length", out IEnumerable<string>? contentLengthValues);
        response.Content.Headers.TryGetValues("Last-Modified", out IEnumerable<string>? lastModifiedValues);
        // response.Content.Headers.TryGetValues("Content-Disposition", out IEnumerable<string>? contentDispositionValues);

        List<string>? values = contentTypeValues?.ToList();
        if (values != null && values.Count != 0)
        {
            contentType = values.First();
        }

        values = contentLengthValues?.ToList();
        if (values != null && values.Count != 0)
        {
            contentLength = long.Parse(values.First(), CultureInfo.CurrentCulture);
        }

        values = lastModifiedValues?.ToList();
        if (values != null && values.Count != 0)
        {
            if (!DateTimeOffset.TryParse(values.First(), out lastModified))
            {
                lastModified = DateTimeOffset.MinValue;
            }
        }

        return (contentType, contentLength, lastModified);
    }

    /// <returns>Document ID</returns>
    private async Task<string> ImportInternalAsync(
        string index,
        DocumentUploadRequest uploadRequest,
        IContext? context,
        CancellationToken cancellationToken)
    {
        // Populate form with values and files from disk
        using MultipartFormDataContent formData = new();

        using StringContent indexContent = new(index);
        using StringContent contextArgsContent = new(JsonSerializer.Serialize(context?.Arguments));
        using (StringContent documentIdContent = new(uploadRequest.DocumentId))
        {
            List<IDisposable> disposables = [];
            formData.Add(indexContent, Constants.WebService.IndexField);
            formData.Add(documentIdContent, Constants.WebService.DocumentIdField);

            if (context?.Arguments != null)
            {
                formData.Add(contextArgsContent, Constants.WebService.ArgsField);
            }

            // Add steps to the form
            foreach (string? step in uploadRequest.Steps)
            {
                if (string.IsNullOrEmpty(step)) { continue; }

                var stepContent = new StringContent(step);
                disposables.Add(stepContent);
                formData.Add(stepContent, Constants.WebService.StepsField);
            }

            // Add tags to the form
            foreach (KeyValuePair<string, string?> tag in uploadRequest.Tags.Pairs)
            {
                var tagContent = new StringContent($"{tag.Key}{Constants.ReservedEqualsChar}{tag.Value}");
                disposables.Add(tagContent);
                formData.Add(tagContent, Constants.WebService.TagsField);
            }

            // Add files to the form
            for (int i = 0; i < uploadRequest.Files.Count; i++)
            {
                if (uploadRequest.Files[i].FileContent is { CanSeek: true, Position: > 0 })
                {
                    uploadRequest.Files[i].FileContent.Seek(0, SeekOrigin.Begin);
                }

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
                var url = Constants.HttpUploadEndpoint.CleanUrlPath();
                HttpResponseMessage response = await this._client.PostAsync(url, formData, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e) when (e.Data.Contains("StatusCode"))
            {
                throw new KernelMemoryWebException($"{e.Message} [StatusCode: {e.Data["StatusCode"]}]", e);
            }
            catch (Exception e)
            {
                throw new KernelMemoryWebException(e.Message, e);
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
