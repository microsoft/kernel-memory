// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.InteractiveSetup.UI;

/// <summary>
/// A boolean that can "change" to True only a limited number of times
/// </summary>
public sealed class BoundedBoolean
{
    private readonly int _maxChangesToTrue;
    private int _changesToTrueCount;
    private bool _value;

    public bool Value
    {
        get
        {
            return this._value;
        }
        set
        {
            if (!value)
            {
                this._value = false;
                return;
            }

            if (this._changesToTrueCount < this._maxChangesToTrue)
            {
                this._changesToTrueCount++;
                this._value = true;
            }
        }
    }

    public BoundedBoolean(bool initialState = false, int maxChangesToTrue = 1)
    {
        this._changesToTrueCount = 0;
        this._maxChangesToTrue = maxChangesToTrue;
        this._value = initialState;
    }
}
