// Copyright (c) Microsoft. All rights reserved.

#if NETSTANDARD2_0

// ReSharper disable CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies that an output is not null even if the corresponding type allows it.
/// Specifies that an input argument was not null when the call returns.
/// See https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.notnullattribute
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class NotNullAttribute : Attribute
{
}
#endif
