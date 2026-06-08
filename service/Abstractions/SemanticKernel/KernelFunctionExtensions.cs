// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel;

/// <summary>
/// Semantic Kernel function extensions.
/// </summary>
public static partial class KernelFunctionExtensions
{
    private const string SemanticFunctionFirstParamName = "input";

    /// <summary>
    /// Invokes the semantic function passing a string in input.
    /// </summary>
    /// <param name="function">Function being invoked</param>
    /// <param name="kernel">Semantic Kernel instance</param>
    /// <param name="text">String input to pass to the function</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns>Result returned by the function call</returns>
    // ReSharper disable once InconsistentNaming
    public static Task<FunctionResult> InvokeAsync(
        this KernelFunction function,
        Kernel kernel,
        string? text,
        CancellationToken cancellationToken = default)
    {
        var args = new KernelArguments();
        if (function.Metadata.Parameters.Count >= 1)
        {
            // Native functions
            args[function.Metadata.Parameters[0].Name] = text;
        }
        else
        {
            // Semantic functions. This works as long as functions follow
            // the convention of using "input" as the first param name.
            args[SemanticFunctionFirstParamName] = text;
        }

        return function.InvokeAsync(kernel, args, cancellationToken);
    }
}
