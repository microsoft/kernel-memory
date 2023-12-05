// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace Microsoft.KernelMemory.SKExtensions;

public static class KernelFunctionExtensions
{
    public static Task<FunctionResult> InvokeWithTextAsync(
        this KernelFunction function,
        Kernel kernel,
        string? text,
        CancellationToken cancellationToken = default)
    {
        var args = new KernelArguments { ["input"] = text };
        return function.InvokeAsync(kernel, args, cancellationToken);
    }
}
