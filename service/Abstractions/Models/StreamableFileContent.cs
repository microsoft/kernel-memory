// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory;

public sealed class StreamableFileContent : IDisposable
{
    private Stream? _stream;

    public string FileName { get; } = string.Empty;
    public long FileSize { get; } = 0;
    public string FileType { get; } = string.Empty;
    public DateTimeOffset LastWrite { get; } = default;
    public Func<Task<Stream>> GetStreamAsync { get; }

    public StreamableFileContent()
    {
        this.GetStreamAsync = () => Task.FromResult<Stream>(new MemoryStream());
    }

    public StreamableFileContent(
        string fileName,
        long fileSize,
        string fileType = "application/octet-stream",
        DateTimeOffset lastWriteTimeUtc = default,
        Func<Task<Stream>>? asyncStreamDelegate = null)
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(fileType, nameof(fileType), "File content type is empty");
        ArgumentNullExceptionEx.ThrowIfNull(lastWriteTimeUtc, nameof(lastWriteTimeUtc), "File last write time is NULL");
        ArgumentNullExceptionEx.ThrowIfNull(asyncStreamDelegate, nameof(asyncStreamDelegate), "asyncStreamDelegate is NULL");

        this.FileName = fileName;
        this.FileSize = fileSize;
        this.FileType = fileType;
        this.LastWrite = lastWriteTimeUtc;
        this.GetStreamAsync = async () =>
        {
            this._stream = await asyncStreamDelegate().ConfigureAwait(false);
            return this._stream;
        };
    }

    public void Dispose()
    {
        if (this._stream == null) { return; }

        this._stream.Close();
        this._stream.Dispose();
    }
}
