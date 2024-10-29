// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
namespace Microsoft.KernelMemory.Evaluators.AnswerCorrectness;

#pragma warning disable CA1812 // 'CorrectnessEvaluation' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic). (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1812)
internal sealed class CorrectnessEvaluation
#pragma warning restore CA1812 // 'CorrectnessEvaluation' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic). (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1812)
{
    public IEnumerable<StatementEvaluation> FP { get; set; } = null!;

    public IEnumerable<StatementEvaluation> FN { get; set; } = null!;

    public IEnumerable<StatementEvaluation> TP { get; set; } = null!;
}
