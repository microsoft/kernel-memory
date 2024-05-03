// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KM.TestHelpers;

namespace Microsoft.Postgres.FunctionalTests;

public class ConcurrencyTests : BaseFunctionalTestCase
{
    public ConcurrencyTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Theory]
    [InlineData("defaultSQL")]
    [InlineData("customSQL")]
    [Trait("Category", "Postgres")]
    public async Task CreateDeleteIndexConcurrencyTest(string type)
    {
        PostgresConfig config;
        switch (type)
        {
            default:
                throw new ArgumentOutOfRangeException(nameof(type), $"Unknown '{type}' test case");

            case "defaultSQL":
                config = this.PostgresConfig;
                break;

            case "customSQL":
                config = new PostgresConfig
                {
                    ConnectionString = this.PostgresConfig.ConnectionString,
                    TableNamePrefix = "custom_sql",
                    Columns = new Dictionary<string, string>()
                    {
                        { "id", "id" },
                        { "embedding", "embedding" },
                        { "tags", "tags" },
                        { "content", "content" },
                        { "payload", "payload" }
                    },
                    CreateTableSql = new List<string>
                    {
                        """
                        BEGIN;
                        SELECT pg_advisory_xact_lock(%%lock_id%%);
                        CREATE TABLE IF NOT EXISTS %%table_name%% (
                            id            TEXT NOT NULL PRIMARY KEY,
                            embedding     vector(%%vector_size%%),
                            tags          TEXT[] DEFAULT '{}'::TEXT[] NOT NULL,
                            content       TEXT DEFAULT '' NOT NULL,
                            payload       JSONB DEFAULT '{}'::JSONB NOT NULL,
                            some_text     TEXT DEFAULT '',
                            last_update   TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
                        );
                        CREATE INDEX IF NOT EXISTS idx_tags ON %%table_name%% USING GIN(tags);
                        COMMIT;
                        """
                    }
                };
                break;
        }

        // ReSharper disable once CommentTypo
        /* If concurrency is not handled properly, the test should fail with
         *   Npgsql.PostgresException
         *   23505: duplicate key value violates unique constraint "pg_type_typname_nsp_index"
         */
        var concurrency = 20;
        var indexName = "create_index_test";
        var vectorSize = 1536;

        using var target = new PostgresMemory(config, new FakeEmbeddingGenerator());

        var tasks = new List<Task>();
        for (int i = 0; i < concurrency; i++)
        {
            tasks.Add(target.CreateIndexAsync(indexName, vectorSize));
        }

        await Task.WhenAll(tasks);

        tasks = new List<Task>();
        for (int i = 0; i < concurrency; i++)
        {
            tasks.Add(target.DeleteIndexAsync(indexName));
        }

        await Task.WhenAll(tasks);
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task UpsertConcurrencyTest()
    {
        var concurrency = 20;
        var vectorSize = 4;
        var indexName = "upsert_test" + Guid.NewGuid().ToString("D");

        using var target = new PostgresMemory(this.PostgresConfig, new FakeEmbeddingGenerator());

        await target.CreateIndexAsync(indexName, vectorSize);

        var record = new MemoryRecord
        {
            Id = "one",
            Vector = new Embedding(new float[] { 0, 1, 2, 3 })
        };

        var tasks = new List<Task>();
        for (int i = 0; i < concurrency; i++)
        {
            tasks.Add(target.UpsertAsync(indexName, record));
        }

        await Task.WhenAll(tasks);

        tasks = new List<Task>();
        for (int i = 0; i < concurrency; i++)
        {
            tasks.Add(target.DeleteAsync(indexName, record));
        }

        await Task.WhenAll(tasks);

        await target.DeleteIndexAsync(indexName);
    }
}
