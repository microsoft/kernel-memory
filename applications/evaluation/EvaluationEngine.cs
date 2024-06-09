// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.Evaluation;

public abstract class EvaluationEngine
{
    protected string GetSKPrompt(string pluginName, string functionName)
    {
        var resourceStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"Prompts/{pluginName}/{functionName}.txt");

        using var reader = new StreamReader(resourceStream!);
        var text = reader.ReadToEnd();
        return text;
    }

    protected async Task<T> Try<T>(int maxCount, Func<int, Task<T>> action)
    {
        do
        {
            try
            {
                return await action(maxCount).ConfigureAwait(false);
            }
            catch (Exception)
            {
                if (maxCount == 0)
                {
                    throw;
                }
            }
        } while (maxCount-- > 0);

        throw new InvalidProgramException();
    }

    /// <summary>
    /// Split records into nodes
    /// </summary>
    /// <param name="records">The records to create nodes.</param>
    /// <param name="count">The number of nodes to create.</param>
    /// <returns></returns>
    protected IEnumerable<MemoryRecord[]> SplitRecordsIntoNodes(MemoryRecord[] records, int count)
    {
        var groups = new List<MemoryRecord[]>();
        var groupSize = (int)Math.Round((double)records.Length / count);

        for (int i = 0; i < count; i++)
        {
            var group = records
                .Skip(i * groupSize)
                .Take(groupSize)
                .ToArray();

            groups.Add(group);
        }

        return groups;
    }

    protected IEnumerable<T> Shuffle<T>(IEnumerable<T> source)
    {
        var span = source.ToArray().AsSpan();

        RandomNumberGenerator.Shuffle(span);

        return span.ToArray();
    }
}
