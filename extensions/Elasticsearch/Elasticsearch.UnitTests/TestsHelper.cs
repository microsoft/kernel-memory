// Copyright (c) Free Mind Labs, Inc. All rights reserved.

using System.Reflection;
using Elastic.Clients.Elasticsearch;
using Microsoft.KernelMemory.MemoryDb.Elasticsearch;

namespace UnitTests;

/// <summary>
/// Extension methods for tests on Elasticsearch.
/// </summary>
internal static class TestsHelper
{
    /// <summary>
    /// Deletes all indices that are created by all test methods of the given class.
    /// Indices must have the same name of a test method to be automatically deleted.
    /// </summary>
    public static async Task<IEnumerable<string>> DeleteIndicesOfTestAsync(this ElasticsearchClient client, Type unitTestType, IIndexNameHelper indexNameHelper)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(unitTestType);
        ArgumentNullException.ThrowIfNull(indexNameHelper);

        // Iterates thru all method names of the test class and deletes the indice with the same name
        var methods = unitTestType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(m =>
                                    (m.GetCustomAttribute<Xunit.FactAttribute>() != null)
                                    ||
                                    (m.GetCustomAttribute<Xunit.TheoryAttribute>() != null)
                                  )
                                  .ToArray();
        if (methods.Length == 0)
        {
            throw new ArgumentException($"No public test methods found in class '{unitTestType.Name}'.");
        }

        var result = new List<string>();
        foreach (var method in methods)
        {
            var indexName = indexNameHelper.Convert(method.Name);
            var delResp = await client.Indices.DeleteAsync(indices: indexName)
                                      .ConfigureAwait(false);

            if (delResp.IsSuccess())
            {
                result.Add(indexName);
            }
        }

        return result;
    }

    ///// <summary>
    ///// Queries the given index for documents until the expected number of documents is found
    ///// or the max number of retries is reached.
    ///// It throws an exception if the expected number of documents is not found.
    ///// </summary>
    //public static async Task WaitForDocumentsAsync(this ElasticsearchClient client, string realIndexName, int expectedDocuments, int maxRetries = 3, int msDelay = 500)
    //{
    //    ArgumentNullException.ThrowIfNull(client);
    //    ArgumentNullException.ThrowIfNull(realIndexName);

    //    return;

    //    var foundCount = 0;
    //    for (int i = 0; i < maxRetries; i++)
    //    {
    //        // We search for all documents
    //        var results = await client
    //            .SearchAsync<ElasticsearchMemoryRecord>(sr =>
    //            {
    //                sr.Index(realIndexName)
    //                  .Query(q => q.MatchAll());
    //            })
    //            .ConfigureAwait(false);

    //        foundCount = results?.HitsMetadata?.Hits?.Count ?? 0;

    //        // If we found all documents, we can return
    //        if ((expectedDocuments == 0) && (foundCount == 0))
    //        {
    //            return;
    //        }
    //        else if (foundCount >= expectedDocuments)
    //        {
    //            return;
    //        }

    //        await Task.Delay(msDelay).ConfigureAwait(false);
    //    }

    //    throw new InvalidOperationException($"It should have inserted {expectedDocuments} documents but only {foundCount}...");
    //}
}
