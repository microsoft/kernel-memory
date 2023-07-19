// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;

public static class Utils
{
    public static bool Contains(this int collection, int x)
    {
        return collection == x;
    }

    public static bool Contains(this int[] collection, int x)
    {
        return Enumerable.Contains(collection.ToArray(), 1);
    }

    public static bool Contains(this Range collection, int x)
    {
        return (x >= collection.Start.Value && x <= collection.End.Value);
    }
}
