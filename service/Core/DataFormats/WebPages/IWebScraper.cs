// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.DataFormats.WebPages;

public interface IWebScraper
{
    /// <summary>
    /// Fetch the content of a web page
    /// </summary>
    /// <param name="url">Web page URL</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Web page content</returns>
    Task<WebScraperResult> GetContentAsync(string url, CancellationToken cancellationToken = default);
}
