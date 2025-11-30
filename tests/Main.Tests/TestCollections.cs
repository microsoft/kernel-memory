// Copyright (c) Microsoft. All rights reserved.
using Xunit;

namespace KernelMemory.Main.Tests;

/// <summary>
/// Collection definition for tests that use Console.Out or Console.Error.
/// Tests in this collection will not run in parallel with each other,
/// preventing output contamination when some tests capture Console output
/// and others write to it directly.
/// </summary>
[CollectionDefinition("ConsoleOutputTests", DisableParallelization = true)]
public class ConsoleOutputTestCollection
{
}
