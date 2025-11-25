// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable CheckNamespace
namespace Microsoft.KernelMemory.Evaluators.ContextRecall;

#pragma warning disable CA1812 // 'GroundTruthClassification' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic). (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1812)
internal sealed class GroundTruthClassification
#pragma warning restore CA1812 // 'GroundTruthClassification' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic). (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1812)
{
    public string Reason { get; set; } = null!;

    public int Attributed { get; set; }

    public string PartitionText { get; set; } = null!;
}
