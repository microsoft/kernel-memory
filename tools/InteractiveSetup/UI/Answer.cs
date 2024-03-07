// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.InteractiveSetup.UI;

public sealed class Answer
{
    public string Name { get; }
    public bool IsSelected { get; }
    public Action OnSelected { get; }

    public Answer(string name, bool isSelected, Action onSelected)
    {
        this.Name = name;
        this.IsSelected = isSelected;
        this.OnSelected = onSelected;
    }
}
