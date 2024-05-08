// Copyright (c) Free Mind Labs, Inc. All rights reserved.

using System.Reflection;
using Elastic.Clients.Elasticsearch;
using Microsoft.KernelMemory.MemoryDb.Elasticsearch;
using Xunit;

namespace Microsoft.Elasticsearch.FunctionalTests.Additional;

/// <summary>
/// Extension methods for tests on Elasticsearch.
/// </summary>
internal static class TestsHelper
{
    /// <summary>
    /// Deletes all indices that are created by all test methods of the given class.
    /// Indices must have the same name of a test method to be automatically deleted.
    /// </summary>
    public static async Task<IEnumerable<string>> DeleteIndicesOfTestAsync(this ElasticsearchClient client, Type unitTestType, IndexNameHelper indexNameHelper)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(unitTestType);
        ArgumentNullException.ThrowIfNull(indexNameHelper);

        // Iterates thru all method names of the test class and deletes the indices with the same name
        var methods = unitTestType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(m =>
                                    m.GetCustomAttribute<FactAttribute>() != null
                                    ||
                                    m.GetCustomAttribute<TheoryAttribute>() != null
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
}
