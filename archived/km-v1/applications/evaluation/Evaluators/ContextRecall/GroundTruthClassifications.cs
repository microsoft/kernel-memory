// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable CheckNamespace
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.Evaluators.ContextRecall;

#pragma warning disable CA1812 // 'GroundTruthClassifications' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic). (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1812)
internal sealed class GroundTruthClassifications
#pragma warning restore CA1812 // 'GroundTruthClassifications' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic). (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1812)
{
    [JsonPropertyName("evaluations")]
    public List<GroundTruthClassification> Evaluations { get; set; } = [];
}
