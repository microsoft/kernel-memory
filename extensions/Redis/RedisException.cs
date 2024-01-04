// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Redis;

public class RedisException : KernelMemoryException
{
    /// <inheritdoc />
    public RedisException() { }

    /// <inheritdoc />
    public RedisException(string message) : base(message) { }

    /// <inheritdoc />
    public RedisException(string message, Exception? innerException) : base(message, innerException) { }
}
