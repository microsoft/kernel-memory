// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticMemory.FileSystem.DevTools;

internal interface ISimpleStorage
{
    Task<string> ReadFileAsTextAsync(string collection, string id, CancellationToken cancellationToken = default);
    Task<BinaryData> ReadFileAsBinaryAsync(string collection, string id, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetAllFileNamesAsync(string collection, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> ReadAllFilesAtTextAsync(string collection, CancellationToken cancellationToken = default);

    Task WriteFileAsync(string collection, string id, string data, CancellationToken cancellationToken = default);
    Task WriteFileAsync(string collection, string id, Stream data, CancellationToken cancellationToken = default);

    Task<bool> FileExistsAsync(string collection, string id, CancellationToken cancellationToken = default);
    Task<bool> CollectionExistsAsync(string collection, string id, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string collection, string id, CancellationToken cancellationToken = default);
    Task DeleteCollectionAsync(string collection, CancellationToken cancellationToken = default);
}
