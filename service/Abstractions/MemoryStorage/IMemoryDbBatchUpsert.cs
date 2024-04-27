﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.KernelMemory.MemoryStorage;

/// <summary>
/// Interface for memory DB adapters supporting batch upsert.
/// The interface is not mandatory and not implemented by all connectors.
/// Handlers/Clients should check if the interface is available and leverage it to optimize throughput.
/// </summary>
public interface IMemoryDbBatchUpsert
{
    /// <summary>
    /// Insert/Update a list of vectors + payload.
    /// </summary>
    /// <param name="index">Index/Collection name</param>
    /// <param name="records">Vectors + payload to save</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns>Record IDs</returns>
    /// <exception cref="IndexNotFound">Error returned if the index where to write doesn't exist</exception>
    IAsyncEnumerable<string> BatchUpsertAsync(
        string index,
        IEnumerable<MemoryRecord> records,
        CancellationToken cancellationToken = default);
}
