// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.Core.Pipeline.Queue.FileBasedQueues;

public class FileBasedQueueConfig
{
    public string Path { get; set; } = "";

    public bool CreateIfNotExist { get; set; } = false;
}
