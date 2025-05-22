// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Qdrant.Client.Grpc;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client;

internal static class QdrantFilter
{
    public static Filter? BuildFilter(IEnumerable<IEnumerable<string>?>? tagGroups)
    {
        if (tagGroups == null)
        {
            return null;
        }

        var list = tagGroups.ToList();
        var filter = new Filter();

        if (list.Count < 2)
        {
            var tags = list.FirstOrDefault();
            if (tags == null)
            {
                return null;
            }

            filter.Must.AddRange(tags.Where(t => !string.IsNullOrEmpty(t)).Select(t => Conditions.MatchText("tags", t)));
            return filter;
        }

        var orFilter = new Filter();
        foreach (var tags in list)
        {
            if (tags == null)
            {
                continue;
            }

            var andFilter = new Filter();
            andFilter.Must.AddRange(tags.Where(t => !string.IsNullOrEmpty(t)).Select(t => Conditions.MatchText("tags", t)));
            orFilter.Should.Add(Conditions.Filter(andFilter));
        }

        filter.Must.Add(Conditions.Filter(orFilter));
        return filter;
    }
}
