// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http.Headers;

namespace Microsoft.KernelMemory.Utils;

#pragma warning disable CA1303
#pragma warning disable CA1812

// TMP workaround for Azure SDK bug
// See https://github.com/Azure/azure-sdk-for-net/issues/46109
internal sealed class AuthFixHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.TryGetValues("Authorization", out var headers) && headers.Count() > 1)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                request.Headers.Authorization!.Scheme,
                request.Headers.Authorization.Parameter);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

internal sealed class HttpLogger : DelegatingHandler
{
    protected async override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Log the request
        Console.WriteLine("## Request:");
        Console.WriteLine(request.ToString());
        if (request.Content != null)
        {
            Console.WriteLine(await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        }

        Console.WriteLine("Headers");
        foreach (var h in request.Headers)
        {
            foreach (string x in h.Value)
            {
                Console.WriteLine($"{h.Key}: {x}");
            }
        }

        Console.WriteLine();

        // Proceed with the request
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // Log the response
        Console.WriteLine("\n\n## Response:");
        Console.WriteLine(response.ToString());
        if (response.Content != null)
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        }

        Console.WriteLine();

        return response;
    }
}
