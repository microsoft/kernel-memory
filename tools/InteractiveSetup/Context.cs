// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup;

internal sealed class Context
{
    public BoundedBoolean CfgWebService = new();

    // Storage
    public BoundedBoolean CfgDocumentStorage = new(initialState: true);
    public BoundedBoolean CfgAzureBlobs = new();
    public BoundedBoolean CfgAWSS3 = new();
    public BoundedBoolean CfgMongoDbAtlasDocumentStorage = new();
    public BoundedBoolean CfgSimpleFileStorage = new();

    // Queues
    public BoundedBoolean CfgQueue = new();
    public BoundedBoolean CfgAzureQueue = new();
    public BoundedBoolean CfgRabbitMq = new();
    public BoundedBoolean CfgSimpleQueues = new();

    // AI
    public BoundedBoolean CfgAzureOpenAIText = new();
    public BoundedBoolean CfgAzureOpenAIEmbedding = new();
    public BoundedBoolean CfgOpenAI = new();
    public BoundedBoolean CfgLlamaSharp = new();
    public BoundedBoolean CfgOllama = new();
    public BoundedBoolean CfgAzureAIDocIntel = new();

    // Vectors
    public BoundedBoolean CfgEmbeddingGenerationEnabled = new(initialState: true);
    public BoundedBoolean CfgAzureAISearch = new();
    public BoundedBoolean CfgMongoDbAtlasMemory = new();
    public BoundedBoolean CfgPostgres = new();
    public BoundedBoolean CfgQdrant = new();
    public BoundedBoolean CfgRedis = new();
    public BoundedBoolean CfgSimpleVectorDb = new();
}
