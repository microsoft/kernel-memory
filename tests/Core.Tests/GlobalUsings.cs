// Copyright (c) Microsoft. All rights reserved.

global using KernelMemory.Core.Embeddings;
global using KernelMemory.Core.Logging;
global using KernelMemory.Core.Search;
global using Xunit;
using System.Diagnostics.CodeAnalysis;

// Test files create disposable objects that are managed by the test framework lifecycle
// These suppressions apply to test code only and are acceptable for unit tests
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
    Justification = "Test objects are disposed when test completes - managed by xUnit")]
[assembly: SuppressMessage("Performance", "CA1861:Prefer 'static readonly' fields over constant array arguments",
    Justification = "Arrays in tests are for readability and small test data")]
