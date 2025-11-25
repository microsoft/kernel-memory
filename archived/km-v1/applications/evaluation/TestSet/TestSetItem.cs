// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.KernelMemory.Evaluation.TestSet;

public sealed class TestSetItem
{
    public string Question { get; set; } = null!;

    public QuestionType QuestionType { get; set; }

    public string GroundTruth { get; set; } = null!;

    public int GroundTruthVerdict { get; set; }

    public IEnumerable<string> Context { get; set; } = [];

    public ICollection<MemoryFilter> Filters { get; set; } = Array.Empty<MemoryFilter>();

    public string ContextString => string.Join("\n", this.Context);
}
