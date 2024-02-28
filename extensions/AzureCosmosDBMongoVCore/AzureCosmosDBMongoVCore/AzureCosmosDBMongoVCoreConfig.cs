// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory;

public class AzureCosmosDBMongoVCoreConfig
{
    public string ConnectionString { get; set; }
    public string IndexName { get; set; }
    public string Kind { get; set; }
    public int NumLists { get; set; }
    public string Similarity { get; set; }
    public int Dimensions { get; set; }
    public int NumberOfConnections { get; set; }
    public int EfConstruction { get; set; }
    public int EfSearch { get; set; }
}