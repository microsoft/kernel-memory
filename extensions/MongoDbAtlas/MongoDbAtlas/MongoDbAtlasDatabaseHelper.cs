// Copyright (c) Microsoft. All rights reserved.

using MongoDB.Driver;

namespace Microsoft.KernelMemory.MongoDbAtlas;

/// <summary>
/// Allow keeping a singleton for IMongoDatabase and MongoClient
/// </summary>
internal static class MongoDbAtlasDatabaseHelper
{
    private static MongoClient? s_client = null;

    internal static IMongoDatabase GetDatabase(string connectionString, string databaseName)
    {
        var builder = new MongoUrlBuilder(connectionString);
        if (!string.IsNullOrEmpty(databaseName))
        {
            builder.DatabaseName = databaseName;
        }

        s_client ??= new MongoClient(builder.ToMongoUrl());

        return s_client.GetDatabase(builder.DatabaseName);
    }
}
