// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.Pipeline;

/// <summary>
/// Generic pipeline exception
/// </summary>
public class PipelineException : KernelMemoryException
{
    /// <inheritdoc />
    public PipelineException() { }

    /// <inheritdoc />
    public PipelineException(string message) : base(message) { }

    /// <inheritdoc />
    public PipelineException(string message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>
/// The pipeline data, e.g. the file storing the pipeline information, is invalid, corrupt
/// </summary>
public class InvalidPipelineDataException : PipelineException
{
    /// <inheritdoc />
    public InvalidPipelineDataException() { }

    /// <inheritdoc />
    public InvalidPipelineDataException(string message) : base(message) { }

    /// <inheritdoc />
    public InvalidPipelineDataException(string message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>
/// The pipeline data file doesn't exist. This might happen if the containing index is deleted.
/// </summary>
public class PipelineNotFoundException : PipelineException
{
    /// <inheritdoc />
    public PipelineNotFoundException() { }

    /// <inheritdoc />
    public PipelineNotFoundException(string message) : base(message) { }

    /// <inheritdoc />
    public PipelineNotFoundException(string message, Exception? innerException) : base(message, innerException) { }
}
