// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Storage;

/// <summary>
/// Interface for generating Cuid2 identifiers.
/// </summary>
public interface ICuidGenerator
{
    /// <summary>
    /// Generates a new lowercase Cuid2 identifier.
    /// </summary>
    /// <returns>A unique lowercase Cuid2 string.</returns>
    string Generate();
}
