// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Chroma.Client;

/// <summary>
/// Represents a singleton implementation of <see cref="HttpClientHandler"/> that is not disposable.
/// </summary>
internal sealed class NonDisposableHttpClientHandler : HttpClientHandler
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NonDisposableHttpClientHandler"/>.
    /// </summary>
    internal static NonDisposableHttpClientHandler Instance { get; } = new();

    /// <summary>
    /// Private constructor to prevent direct instantiation of the class.
    /// </summary>
    private NonDisposableHttpClientHandler()
    {
        this.CheckCertificateRevocationList = true;
    }

#pragma warning disable CA2215 // nothing to dispose
    /// <summary>
    /// Disposes the underlying resources.
    /// This implementation does nothing to prevent unintended disposal, as it may affect all references.
    /// </summary>
    /// <param name="disposing">True if called from <see cref="Dispose"/>, false if called from a finalizer.</param>
    protected override void Dispose(bool disposing)
    {
        // Do nothing if called explicitly from Dispose, as it may unintentionally affect all references.
        // The base.Dispose(disposing) is not called to avoid invoking the disposal of HttpClientHandler resources.
        // This implementation assumes that the HttpClientHandler is being used as a singleton and should not be disposed directly.
    }
#pragma warning restore CA2215
}
