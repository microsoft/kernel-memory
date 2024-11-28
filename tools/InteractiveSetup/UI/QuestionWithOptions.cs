// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.KernelMemory.InteractiveSetup.UI;

public sealed class QuestionWithOptions
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Answer> Options { get; set; } = [];
}
