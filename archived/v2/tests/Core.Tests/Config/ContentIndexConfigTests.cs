// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Tests.Config;

/// <summary>
/// Tests for Content Index configuration validation
/// </summary>
public sealed class ContentIndexConfigTests
{
    [Fact]
    public void LoadFromFile_WithPostgresContentIndex_ShouldValidate()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        const string json = @"{
            ""nodes"": {
                ""test"": {
                    ""id"": ""test"",
                    ""access"": ""Full"",
                    ""contentIndex"": {
                        ""type"": ""postgres"",
                        ""connectionString"": ""Host=localhost;Database=testdb;Username=test;Password=test""
                    }
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act
            var config = ConfigParser.LoadFromFile(tempFile);

            // Assert
            Assert.NotNull(config);
            var node = config.Nodes["test"];
            var postgresIndex = Assert.IsType<KernelMemory.Core.Config.ContentIndex.PostgresContentIndexConfig>(node.ContentIndex);
            Assert.Equal("Host=localhost;Database=testdb;Username=test;Password=test", postgresIndex.ConnectionString);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadFromFile_WithPostgresContentIndexMissingConnectionString_ShouldThrowConfigException()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid()}.json");
        const string json = @"{
            ""nodes"": {
                ""test"": {
                    ""id"": ""test"",
                    ""access"": ""Full"",
                    ""contentIndex"": {
                        ""type"": ""postgres"",
                        ""connectionString"": """"
                    }
                }
            }
        }";

        try
        {
            File.WriteAllText(tempFile, json);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => ConfigParser.LoadFromFile(tempFile));
            Assert.Contains("PostgreSQL connection string is required", exception.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
