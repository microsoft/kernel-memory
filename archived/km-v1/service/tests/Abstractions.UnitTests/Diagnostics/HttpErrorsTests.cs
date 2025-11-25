// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KM.Abstractions.UnitTests.Diagnostics;

public sealed class HttpErrorsTests
{
    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItRecognizesErrorsFromNulls()
    {
        HttpStatusCode? statusCode = null;

        Assert.False(statusCode.IsTransientError());
        Assert.False(statusCode.IsFatalError());
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(HttpStatusCode.Continue)] // 100
    [InlineData(HttpStatusCode.SwitchingProtocols)] // 101
    [InlineData(HttpStatusCode.Processing)] // 102
    [InlineData(HttpStatusCode.EarlyHints)] // 103
    [InlineData(HttpStatusCode.OK)] // 200
    [InlineData(HttpStatusCode.Created)] // 201
    [InlineData(HttpStatusCode.Accepted)] // 202
    [InlineData(HttpStatusCode.NonAuthoritativeInformation)] // 203
    [InlineData(HttpStatusCode.NoContent)] // 204
    [InlineData(HttpStatusCode.ResetContent)] // 205
    [InlineData(HttpStatusCode.Ambiguous)] // 300
    [InlineData(HttpStatusCode.Moved)] // 301
    [InlineData(HttpStatusCode.Found)] // 302
    [InlineData(HttpStatusCode.RedirectMethod)] // 303
    [InlineData(HttpStatusCode.NotModified)] // 304
    [InlineData(HttpStatusCode.UseProxy)] // 305
    [InlineData(HttpStatusCode.Unused)] // 306
    [InlineData(HttpStatusCode.RedirectKeepVerb)] // 307
    [InlineData(HttpStatusCode.PermanentRedirect)] // 308
    public void ItRecognizesErrors1(HttpStatusCode statusCode)
    {
        Assert.False(statusCode.IsTransientError());
        Assert.False(HttpErrors.IsTransientError((int)statusCode));

        Assert.False(statusCode.IsFatalError());
        Assert.False(HttpErrors.IsFatalError((int)statusCode));
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(HttpStatusCode.Continue)] // 100
    [InlineData(HttpStatusCode.SwitchingProtocols)] // 101
    [InlineData(HttpStatusCode.Processing)] // 102
    [InlineData(HttpStatusCode.EarlyHints)] // 103
    [InlineData(HttpStatusCode.OK)] // 200
    [InlineData(HttpStatusCode.Created)] // 201
    [InlineData(HttpStatusCode.Accepted)] // 202
    [InlineData(HttpStatusCode.NonAuthoritativeInformation)] // 203
    [InlineData(HttpStatusCode.NoContent)] // 204
    [InlineData(HttpStatusCode.ResetContent)] // 205
    [InlineData(HttpStatusCode.Ambiguous)] // 300
    [InlineData(HttpStatusCode.Moved)] // 301
    [InlineData(HttpStatusCode.Found)] // 302
    [InlineData(HttpStatusCode.RedirectMethod)] // 303
    [InlineData(HttpStatusCode.NotModified)] // 304
    [InlineData(HttpStatusCode.UseProxy)] // 305
    [InlineData(HttpStatusCode.Unused)] // 306
    [InlineData(HttpStatusCode.RedirectKeepVerb)] // 307
    [InlineData(HttpStatusCode.PermanentRedirect)] // 308
    public void ItRecognizesErrors2(HttpStatusCode? statusCode)
    {
        Assert.False(statusCode.IsTransientError());
        Assert.False(statusCode.IsFatalError());
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(HttpStatusCode.RequestTimeout)] // 408
    [InlineData(HttpStatusCode.PreconditionFailed)] // 412
    [InlineData(HttpStatusCode.Locked)] // 423
    [InlineData(HttpStatusCode.TooManyRequests)] // 429
    [InlineData(HttpStatusCode.InternalServerError)] // 500
    [InlineData(HttpStatusCode.BadGateway)] // 502
    [InlineData(HttpStatusCode.ServiceUnavailable)] // 503
    [InlineData(HttpStatusCode.GatewayTimeout)] // 504
    [InlineData(HttpStatusCode.InsufficientStorage)] // 507
    public void ItRecognizesTransientErrors1(HttpStatusCode statusCode)
    {
        Assert.True(statusCode.IsTransientError());
        Assert.True(HttpErrors.IsTransientError((int)statusCode));

        Assert.False(statusCode.IsFatalError());
        Assert.False(HttpErrors.IsFatalError((int)statusCode));
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(HttpStatusCode.RequestTimeout)] // 408
    [InlineData(HttpStatusCode.PreconditionFailed)] // 412
    [InlineData(HttpStatusCode.Locked)] // 423
    [InlineData(HttpStatusCode.TooManyRequests)] // 429
    [InlineData(HttpStatusCode.InternalServerError)] // 500
    [InlineData(HttpStatusCode.BadGateway)] // 502
    [InlineData(HttpStatusCode.ServiceUnavailable)] // 503
    [InlineData(HttpStatusCode.GatewayTimeout)] // 504
    [InlineData(HttpStatusCode.InsufficientStorage)] // 507
    public void ItRecognizesTransientErrors2(HttpStatusCode? statusCode)
    {
        Assert.True(statusCode.IsTransientError());
        Assert.False(statusCode.IsFatalError());
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(HttpStatusCode.BadRequest)] // 400
    [InlineData(HttpStatusCode.Unauthorized)] // 401
    [InlineData(HttpStatusCode.PaymentRequired)] // 402
    [InlineData(HttpStatusCode.Forbidden)] // 403
    [InlineData(HttpStatusCode.NotFound)] // 404
    [InlineData(HttpStatusCode.MethodNotAllowed)] // 405
    [InlineData(HttpStatusCode.NotAcceptable)] // 406
    [InlineData(HttpStatusCode.ProxyAuthenticationRequired)] // 407
    [InlineData(HttpStatusCode.Conflict)] // 409
    [InlineData(HttpStatusCode.Gone)] // 410
    [InlineData(HttpStatusCode.LengthRequired)] // 411
    [InlineData(HttpStatusCode.RequestEntityTooLarge)] // 413
    [InlineData(HttpStatusCode.RequestUriTooLong)] // 414
    [InlineData(HttpStatusCode.UnsupportedMediaType)] // 415
    [InlineData(HttpStatusCode.RequestedRangeNotSatisfiable)] // 416
    [InlineData(HttpStatusCode.ExpectationFailed)] // 417
    [InlineData(HttpStatusCode.UnprocessableContent)] // 422
    [InlineData(HttpStatusCode.UpgradeRequired)] // 426
    [InlineData(HttpStatusCode.RequestHeaderFieldsTooLarge)] // 431
    [InlineData(HttpStatusCode.UnavailableForLegalReasons)] // 451
    [InlineData(HttpStatusCode.NotImplemented)] // 501
    [InlineData(HttpStatusCode.HttpVersionNotSupported)] // 505
    [InlineData(HttpStatusCode.LoopDetected)] // 508
    [InlineData(HttpStatusCode.NotExtended)] // 510
    [InlineData(HttpStatusCode.NetworkAuthenticationRequired)] // 511
    public void ItRecognizesFatalErrors1(HttpStatusCode statusCode)
    {
        Assert.False(statusCode.IsTransientError());
        Assert.False(HttpErrors.IsTransientError((int)statusCode));

        Assert.True(statusCode.IsFatalError());
        Assert.True(HttpErrors.IsFatalError((int)statusCode));
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData(HttpStatusCode.BadRequest)] // 400
    [InlineData(HttpStatusCode.Unauthorized)] // 401
    [InlineData(HttpStatusCode.PaymentRequired)] // 402
    [InlineData(HttpStatusCode.Forbidden)] // 403
    [InlineData(HttpStatusCode.NotFound)] // 404
    [InlineData(HttpStatusCode.MethodNotAllowed)] // 405
    [InlineData(HttpStatusCode.NotAcceptable)] // 406
    [InlineData(HttpStatusCode.ProxyAuthenticationRequired)] // 407
    [InlineData(HttpStatusCode.Conflict)] // 409
    [InlineData(HttpStatusCode.Gone)] // 410
    [InlineData(HttpStatusCode.LengthRequired)] // 411
    [InlineData(HttpStatusCode.RequestEntityTooLarge)] // 413
    [InlineData(HttpStatusCode.RequestUriTooLong)] // 414
    [InlineData(HttpStatusCode.UnsupportedMediaType)] // 415
    [InlineData(HttpStatusCode.RequestedRangeNotSatisfiable)] // 416
    [InlineData(HttpStatusCode.ExpectationFailed)] // 417
    [InlineData(HttpStatusCode.UnprocessableContent)] // 422
    [InlineData(HttpStatusCode.UpgradeRequired)] // 426
    [InlineData(HttpStatusCode.RequestHeaderFieldsTooLarge)] // 431
    [InlineData(HttpStatusCode.UnavailableForLegalReasons)] // 451
    [InlineData(HttpStatusCode.NotImplemented)] // 501
    [InlineData(HttpStatusCode.HttpVersionNotSupported)] // 505
    [InlineData(HttpStatusCode.LoopDetected)] // 508
    [InlineData(HttpStatusCode.NotExtended)] // 510
    [InlineData(HttpStatusCode.NetworkAuthenticationRequired)] // 511
    public void ItRecognizesFatalErrors2(HttpStatusCode? statusCode)
    {
        Assert.False(statusCode.IsTransientError());
        Assert.True(statusCode.IsFatalError());
    }
}
