// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Configure the builder
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="action">Action to use to configure the builder</param>
    /// <returns>Builder instance</returns>
    public static IKernelMemoryBuilder Configure(
        this IKernelMemoryBuilder builder,
        Action<IKernelMemoryBuilder> action)
    {
        action.Invoke(builder);
        return builder;
    }

    /// <summary>
    /// Configure the builder in one of two ways, depending on a condition
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="condition">Condition to check</param>
    /// <param name="actionIfTrue">How to configure the builder when the condition is true</param>
    /// <param name="actionIfFalse">Optional, how to configure the builder when the condition is false</param>
    /// <returns>Builder instance</returns>
    public static IKernelMemoryBuilder Configure(
        this IKernelMemoryBuilder builder,
        bool condition,
        Action<IKernelMemoryBuilder> actionIfTrue,
        Action<IKernelMemoryBuilder>? actionIfFalse = null)
    {
        if (condition)
        {
            actionIfTrue.Invoke(builder);
        }
        else
        {
            actionIfFalse?.Invoke(builder);
        }

        return builder;
    }
}
