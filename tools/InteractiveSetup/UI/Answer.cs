// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.InteractiveSetup.UI;

public sealed class Answer
{
    public string Name { get; }
    public Action Selected { get; }

    public Answer(string name, Action selected)
    {
        this.Name = name;
        this.Selected = selected;
    }
}
