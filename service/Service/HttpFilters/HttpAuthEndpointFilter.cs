// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.KernelMemory.Configuration;

namespace Microsoft.KernelMemory.Service;

public sealed class HttpAuthEndpointFilter : IEndpointFilter
{
    private readonly ServiceAuthorizationConfig _config;

    public HttpAuthEndpointFilter(ServiceAuthorizationConfig config)
    {
        config.Validate();
        this._config = config;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (this._config.Enabled)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(this._config.HttpHeaderName, out var apiKey))
            {
                return Results.Problem(detail: "Missing API Key HTTP header", statusCode: 401);
            }

            if (!string.Equals(apiKey, this._config.AccessKey1, StringComparison.Ordinal)
                && !string.Equals(apiKey, this._config.AccessKey2, StringComparison.Ordinal))
            {
                return Results.Problem(detail: "Invalid API Key", statusCode: 403);
            }
        }

        return await next(context);
    }
}
