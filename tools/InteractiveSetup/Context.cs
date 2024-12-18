// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup;

internal sealed class Context
{
    public readonly BoundedBoolean CfgWebService = new();

    // Storage
    public readonly BoundedBoolean CfgDocumentStorage = new(initialState: true);
    public readonly BoundedBoolean CfgAzureBlobs = new();
    public readonly BoundedBoolean CfgAWSS3 = new();
    public readonly BoundedBoolean CfgMongoDbAtlasDocumentStorage = new();
    public readonly BoundedBoolean CfgSimpleFileStorageVolatile = new();
    public readonly BoundedBoolean CfgSimpleFileStoragePersistent = new();

    // Queues
    public readonly BoundedBoolean CfgQueue = new();
    public readonly BoundedBoolean CfgAzureQueue = new();
    public readonly BoundedBoolean CfgRabbitMq = new();
    public readonly BoundedBoolean CfgSimpleQueuesVolatile = new();
    public readonly BoundedBoolean CfgSimpleQueuesPersistent = new();

    // AI
    public readonly BoundedBoolean CfgAzureOpenAIText = new();
    public readonly BoundedBoolean CfgAzureOpenAIEmbedding = new();
    public readonly BoundedBoolean CfgOpenAI = new();
    public readonly BoundedBoolean CfgLlamaSharpEmbedding = new();
    public readonly BoundedBoolean CfgLlamaSharpText = new();
    public readonly BoundedBoolean CfgOllamaEmbedding = new();
    public readonly BoundedBoolean CfgOllamaText = new();
    public readonly BoundedBoolean CfgAzureAIDocIntel = new();

    // Vectors
    public readonly BoundedBoolean CfgEmbeddingGenerationEnabled = new(initialState: true);
    public readonly BoundedBoolean CfgAzureAISearch = new();
    public readonly BoundedBoolean CfgMongoDbAtlasMemory = new();
    public readonly BoundedBoolean CfgPostgres = new();
    public readonly BoundedBoolean CfgQdrant = new();
    public readonly BoundedBoolean CfgRedis = new();
    public readonly BoundedBoolean CfgSimpleVectorDbVolatile = new();
    public readonly BoundedBoolean CfgSimpleVectorDbPersistent = new();
}
