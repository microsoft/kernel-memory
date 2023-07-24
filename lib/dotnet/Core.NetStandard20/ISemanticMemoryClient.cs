// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;

// ReSharper disable CommentTypo

namespace Microsoft.SemanticKernel.SemanticMemory.Core20;

public interface ISemanticMemoryClient
{
    public Task ImportFileAsync(string file, ImportFileOptions options);
    public Task ImportFilesAsync(string[] files, ImportFileOptions options);
}
