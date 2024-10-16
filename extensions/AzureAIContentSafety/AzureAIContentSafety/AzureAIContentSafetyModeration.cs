// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.ContentSafety;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.DocumentStorage;

namespace Microsoft.KernelMemory.Safety.AzureAIContentSafety;

[Experimental("KMEXP05")]
public class AzureAIContentSafetyModeration : IContentModeration
{
    private const string Replacement = "****";

    private readonly ContentSafetyClient _client;
    private readonly ILogger<AzureAIContentSafetyModeration> _log;
    private readonly double _globalSafetyThreshold;
    private readonly List<string> _ignoredWords;

    public AzureAIContentSafetyModeration(
        AzureAIContentSafetyConfig config,
        ILoggerFactory? loggerFactory = null)
    {
        config.Validate();
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<AzureAIContentSafetyModeration>();
        this._globalSafetyThreshold = config.GlobalSafetyThreshold;
        this._ignoredWords = config.IgnoredWords;

        switch (config.Auth)
        {
            case AzureAIContentSafetyConfig.AuthTypes.AzureIdentity:
                this._client = new ContentSafetyClient(new Uri(config.Endpoint), new DefaultAzureCredential());
                break;

            case AzureAIContentSafetyConfig.AuthTypes.APIKey:
                this._client = new ContentSafetyClient(new Uri(config.Endpoint), new AzureKeyCredential(config.APIKey));
                break;

            case AzureAIContentSafetyConfig.AuthTypes.ManualTokenCredential:
                this._client = new ContentSafetyClient(new Uri(config.Endpoint), config.GetTokenCredential());
                break;

            default:
                this._log.LogCritical("Azure AI Search authentication type '{0}' undefined or not supported", config.Auth);
                throw new DocumentStorageException($"Azure AI Search authentication type '{config.Auth}' undefined or not supported");
        }
    }

    /// <inheritdoc/>
    public Task<bool> IsSafeAsync(string? text, CancellationToken cancellationToken)
    {
        return this.IsSafeAsync(text, this._globalSafetyThreshold, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsSafeAsync(string? text, double threshold, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text)) { return true; }

        text = this.RemoveIgnoredWords(text);

        Response<AnalyzeTextResult>? result = await this._client.AnalyzeTextAsync(text, cancellationToken).ConfigureAwait(false);

        bool isSafe = result.HasValue && result.Value.CategoriesAnalysis.All(x => x.Severity <= threshold);

        if (!isSafe)
        {
            IEnumerable<string> report = result.HasValue ? result.Value.CategoriesAnalysis.Select(x => $"{x.Category}: {x.Severity}") : Array.Empty<string>();
            this._log.LogWarning("Unsafe content detected, report: {0}", string.Join("; ", report));
            this._log.LogSensitive("Unsafe content: {0}", text);
        }

        return isSafe;
    }

    private string RemoveIgnoredWords(string text)
    {
        foreach (var word in this._ignoredWords)
        {
            text = ReplaceWholeWordIgnoreCase(text, word, Replacement);
        }

        return text;
    }

    private static string ReplaceWholeWordIgnoreCase(string text, string oldValue, string newValue)
    {
        // \b ensures word boundaries, Regex.Escape escapes any special characters in oldValue
        return Regex.Replace(text, $@"\b{Regex.Escape(oldValue)}\b", newValue, RegexOptions.IgnoreCase);
    }
}
