// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup.Doctor;

internal static class ListTupleExtensions
{
    public static string Get(this List<Tuple<string, string>> list, string key)
    {
        foreach (var kv in list.Where(kv => kv.Item1 == key))
        {
            return kv.Item2;
        }

        return string.Empty;
    }

    public static List<Tuple<string, string>> Add(this List<Tuple<string, string>> list, string key, string value)
    {
        list.Add(new Tuple<string, string>(key, value));
        return list;
    }

    public static List<Tuple<string, string>> AddSeparator(this List<Tuple<string, string>> list)
    {
        return Add(list, "", "");
    }
}
