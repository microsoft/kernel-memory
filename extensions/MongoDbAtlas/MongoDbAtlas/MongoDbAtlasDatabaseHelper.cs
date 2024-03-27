// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using MongoDB.Driver;

namespace Microsoft.KernelMemory.MongoDbAtlas;

/// <summary>
/// Allow keeping a singleton for IMongoDatabase and MongoClient
/// </summary>
internal static class MongoDbAtlasDatabaseHelper
{
    private static readonly ConcurrentDictionary<string, IMongoClient> s_mongoClientCache = new();

    internal static IMongoDatabase GetDatabase(string connectionString, string databaseName)
    {
        IMongoClient? client = GetClient(connectionString);

        var builder = new MongoUrlBuilder(connectionString);
        if (!string.IsNullOrEmpty(databaseName))
        {
            builder.DatabaseName = databaseName;
        }

        return client.GetDatabase(builder.DatabaseName);
    }

    internal static IMongoClient GetClient(string connectionString)
    {
        var connectionKey = GetConnectionKey(connectionString);
        if (!s_mongoClientCache.TryGetValue(connectionKey, out var client))
        {
            //never encountered this connection string before, create a new client
            client = new MongoClient(connectionString);
            s_mongoClientCache.TryAdd(connectionKey, client);
        }

        return client;
    }

    /// <summary>
    /// From a connection string to MongoDB it get a key that can uniquely identify the
    /// connection. Usually the key is related to the host part of the connection string.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    private static string GetConnectionKey(string connectionString)
    {
        MongoUrlBuilder mongoUrlBuilder = new(connectionString);
        // Now we can simply use the host and the username to create a unique key
        return $"{mongoUrlBuilder.Server.Host}:{mongoUrlBuilder.Server.Port}:{mongoUrlBuilder.Username ?? ""}";
    }
}
