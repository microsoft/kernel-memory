// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.Prompts;

public interface IPromptSupplier
{
    string ReadPrompt(string filename);
}
