// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.Evaluation.TestSet;

#pragma warning disable CA1812 // 'QuestionAnswer' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic). (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1812)
internal sealed class QuestionAnswer
#pragma warning restore CA1812 // 'QuestionAnswer' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic). (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1812)
{
    public string Answer { get; set; } = null!;

    public int Verdict { get; set; }
}
