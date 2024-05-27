﻿// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable CheckNamespace

namespace Microsoft.KernelMemory.Evaluators.ContextRelevancy;

#pragma warning disable CA1812 // 'ContextRelevancy' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic). (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1812)
internal sealed class ContextRelevancy
#pragma warning restore CA1812 // 'ContextRelevancy' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic). (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1812)
{
    public string Reason { get; set; } = null!;

    public int Verdict { get; set; }

    public string PartitionText { get; set; } = null!;
}
