// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.Pipeline.Queue.DevTools;
using Microsoft.KernelMemory.Safety.AzureAIContentSafety;
using Microsoft.KM.TestHelpers;

namespace Microsoft.KM.Core.UnitTests.Configuration;

[Trait("Category", "UnitTest")]
public class ConfigEnvVarsTest : BaseUnitTestCase
{
    public ConfigEnvVarsTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void ItIgnoresDefaultValues()
    {
        // Arrange
        var target = new AzureAIContentSafetyConfig();

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObjectNoDefaults(
            target, "KernelMemory", "Services", "AzureAIContentSafety");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ItWorksForAzureAIContentSafetyConfig()
    {
        // Arrange
        var target = new AzureAIContentSafetyConfig
        {
            Auth = AzureAIContentSafetyConfig.AuthTypes.APIKey,
            Endpoint = "http://endpoint",
            APIKey = "xyz",
            GlobalSafetyThreshold = 0.35,
            IgnoredWords = ["foo", "bar"]
        };

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(
            target, "KernelMemory", "Services", "AzureAIContentSafety");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Equal(6, result.Count);
        Assert.Equal("APIKey", result["KernelMemory__Services__AzureAIContentSafety__Auth"]);
        Assert.Equal("http://endpoint", result["KernelMemory__Services__AzureAIContentSafety__Endpoint"]);
        Assert.Equal("xyz", result["KernelMemory__Services__AzureAIContentSafety__APIKey"]);
        Assert.Equal("0.35", result["KernelMemory__Services__AzureAIContentSafety__GlobalSafetyThreshold"]);
        Assert.Equal("foo", result["KernelMemory__Services__AzureAIContentSafety__IgnoredWords__0"]);
        Assert.Equal("bar", result["KernelMemory__Services__AzureAIContentSafety__IgnoredWords__1"]);
    }

    [Fact]
    public void ItWorksForAzureAIDocIntelConfig()
    {
        // Arrange
        var target = new AzureAIDocIntelConfig
        {
            Auth = AzureAIDocIntelConfig.AuthTypes.APIKey,
            Endpoint = "http://endpoint",
            APIKey = "xyz",
        };

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(
            target, "KernelMemory", "Services", "AzureAIDocIntel");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("APIKey", result["KernelMemory__Services__AzureAIDocIntel__Auth"]);
        Assert.Equal("http://endpoint", result["KernelMemory__Services__AzureAIDocIntel__Endpoint"]);
        Assert.Equal("xyz", result["KernelMemory__Services__AzureAIDocIntel__APIKey"]);
    }

    [Fact]
    public void ItWorksForAzureAISearchConfig()
    {
        // Arrange
        var target = new AzureAISearchConfig
        {
            Auth = AzureAISearchConfig.AuthTypes.APIKey,
            Endpoint = "http://endpoint",
            APIKey = "xyz",
            UseHybridSearch = true,
            UseStickySessions = true,
        };

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(
            target, "KernelMemory", "Services", "AzureAISearch");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal("APIKey", result["KernelMemory__Services__AzureAISearch__Auth"]);
        Assert.Equal("http://endpoint", result["KernelMemory__Services__AzureAISearch__Endpoint"]);
        Assert.Equal("xyz", result["KernelMemory__Services__AzureAISearch__APIKey"]);
        Assert.Equal("True", result["KernelMemory__Services__AzureAISearch__UseHybridSearch"]);
        Assert.Equal("True", result["KernelMemory__Services__AzureAISearch__UseStickySessions"]);
    }

    [Fact]
    public void ItWorksForAzureBlobsConfig()
    {
        // Arrange
        var target = new AzureBlobsConfig
        {
            Auth = AzureBlobsConfig.AuthTypes.ConnectionString,
            ConnectionString = "http://endpoint",
            Account = "acct",
            AccountKey = "acctKey",
            Container = "container name",
            EndpointSuffix = "sfx",
        };

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(
            target, "KernelMemory", "Services", "AzureBlobs");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Equal(6, result.Count);
        Assert.Equal("ConnectionString", result["KernelMemory__Services__AzureBlobs__Auth"]);
        Assert.Equal("http://endpoint", result["KernelMemory__Services__AzureBlobs__ConnectionString"]);
        Assert.Equal("acct", result["KernelMemory__Services__AzureBlobs__Account"]);
        Assert.Equal("acctKey", result["KernelMemory__Services__AzureBlobs__AccountKey"]);
        Assert.Equal("container name", result["KernelMemory__Services__AzureBlobs__Container"]);
        Assert.Equal("sfx", result["KernelMemory__Services__AzureBlobs__EndpointSuffix"]);
    }

    [Fact]
    public void ItWorksForAzureOpenAIConfig()
    {
        // Arrange
        var target = new AzureOpenAIConfig
        {
            APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
            Auth = AzureOpenAIConfig.AuthTypes.APIKey,
            APIKey = "x y z",
            Endpoint = "http://endpoint",
            Deployment = "gpt",
            MaxTokenTotal = 9000,
            Tokenizer = "o200k",
            EmbeddingDimensions = 1000,
            MaxEmbeddingBatchSize = 10,
            MaxRetries = 5,
            TrustedCertificateThumbprints = ["abc", "bb"],
        };

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(
            target, "KernelMemory", "Services", "AzureOpenAIxyz");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Equal(12, result.Count);
        Assert.Equal("ChatCompletion", result["KernelMemory__Services__AzureOpenAIxyz__APIType"]);
        Assert.Equal("APIKey", result["KernelMemory__Services__AzureOpenAIxyz__Auth"]);
        Assert.Equal("http://endpoint", result["KernelMemory__Services__AzureOpenAIxyz__Endpoint"]);
        Assert.Equal("gpt", result["KernelMemory__Services__AzureOpenAIxyz__Deployment"]);
        Assert.Equal("9000", result["KernelMemory__Services__AzureOpenAIxyz__MaxTokenTotal"]);
        Assert.Equal("o200k", result["KernelMemory__Services__AzureOpenAIxyz__Tokenizer"]);
        Assert.Equal("1000", result["KernelMemory__Services__AzureOpenAIxyz__EmbeddingDimensions"]);
        Assert.Equal("10", result["KernelMemory__Services__AzureOpenAIxyz__MaxEmbeddingBatchSize"]);
        Assert.Equal("5", result["KernelMemory__Services__AzureOpenAIxyz__MaxRetries"]);
        Assert.Equal("abc", result["KernelMemory__Services__AzureOpenAIxyz__TrustedCertificateThumbprints__0"]);
        Assert.Equal("bb", result["KernelMemory__Services__AzureOpenAIxyz__TrustedCertificateThumbprints__1"]);
    }

    [Fact]
    public void ItWorksForAzureQueuesConfig()
    {
        // Arrange
        var target = new AzureQueuesConfig
        {
            Auth = AzureQueuesConfig.AuthTypes.ConnectionString,
            ConnectionString = "http://endpoint",
            Account = "acct",
            AccountKey = "acctKey",
            EndpointSuffix = "sfx",
            PollDelayMsecs = 5,
            FetchBatchSize = 6,
            FetchLockSeconds = 7,
            MaxRetriesBeforePoisonQueue = 8,
            PoisonQueueSuffix = "dl",
        };

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(
            target, "KernelMemory", "Services", "AzureQueues");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal("ConnectionString", result["KernelMemory__Services__AzureQueues__Auth"]);
        Assert.Equal("http://endpoint", result["KernelMemory__Services__AzureQueues__ConnectionString"]);
        Assert.Equal("acct", result["KernelMemory__Services__AzureQueues__Account"]);
        Assert.Equal("acctKey", result["KernelMemory__Services__AzureQueues__AccountKey"]);
        Assert.Equal("sfx", result["KernelMemory__Services__AzureQueues__EndpointSuffix"]);
        Assert.Equal("5", result["KernelMemory__Services__AzureQueues__PollDelayMsecs"]);
        Assert.Equal("6", result["KernelMemory__Services__AzureQueues__FetchBatchSize"]);
        Assert.Equal("7", result["KernelMemory__Services__AzureQueues__FetchLockSeconds"]);
        Assert.Equal("8", result["KernelMemory__Services__AzureQueues__MaxRetriesBeforePoisonQueue"]);
        Assert.Equal("dl", result["KernelMemory__Services__AzureQueues__PoisonQueueSuffix"]);
    }

    [Fact]
    public void ItWorksForOpenAIConfig()
    {
        // Arrange
        var target = new OpenAIConfig
        {
            TextGenerationType = OpenAIConfig.TextGenerationTypes.Chat,
            APIKey = "key",
            OrgId = "org",
            Endpoint = "openai.com",
            TextModel = "dv",
            TextModelMaxTokenTotal = 100,
            TextModelTokenizer = "o200k",
            EmbeddingModel = "ada",
            EmbeddingModelMaxTokenTotal = 200,
            EmbeddingModelTokenizer = "cl100k",
            EmbeddingDimensions = 444,
            MaxEmbeddingBatchSize = 20,
            MaxRetries = 8
        };

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(
            target, "KernelMemory", "Services", "OpenAI");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Equal(13, result.Count);
        Assert.Equal("Chat", result["KernelMemory__Services__OpenAI__TextGenerationType"]);
        Assert.Equal("key", result["KernelMemory__Services__OpenAI__APIKey"]);
        Assert.Equal("org", result["KernelMemory__Services__OpenAI__OrgId"]);
        Assert.Equal("openai.com", result["KernelMemory__Services__OpenAI__Endpoint"]);
        Assert.Equal("dv", result["KernelMemory__Services__OpenAI__TextModel"]);
        Assert.Equal("100", result["KernelMemory__Services__OpenAI__TextModelMaxTokenTotal"]);
        Assert.Equal("o200k", result["KernelMemory__Services__OpenAI__TextModelTokenizer"]);
        Assert.Equal("ada", result["KernelMemory__Services__OpenAI__EmbeddingModel"]);
        Assert.Equal("200", result["KernelMemory__Services__OpenAI__EmbeddingModelMaxTokenTotal"]);
        Assert.Equal("cl100k", result["KernelMemory__Services__OpenAI__EmbeddingModelTokenizer"]);
        Assert.Equal("444", result["KernelMemory__Services__OpenAI__EmbeddingDimensions"]);
        Assert.Equal("20", result["KernelMemory__Services__OpenAI__MaxEmbeddingBatchSize"]);
        Assert.Equal("8", result["KernelMemory__Services__OpenAI__MaxRetries"]);
    }

    [Fact]
    public void ItWorksForSimpleFileStorageConfig()
    {
        // Arrange
        var target = new SimpleFileStorageConfig
        {
            StorageType = FileSystemTypes.Disk,
            Directory = "c:/"
        };

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(
            target, "KernelMemory", "Services", "SimpleFileStorage");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Disk", result["KernelMemory__Services__SimpleFileStorage__StorageType"]);
        Assert.Equal("c:/", result["KernelMemory__Services__SimpleFileStorage__Directory"]);
    }

    [Fact]
    public void ItWorksForSimpleVectorDbConfig()
    {
        // Arrange
        var target = new SimpleVectorDbConfig
        {
            StorageType = FileSystemTypes.Disk,
            Directory = "c:/"
        };

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(
            target, "KernelMemory", "Services", "SimpleVectorDb");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Disk", result["KernelMemory__Services__SimpleVectorDb__StorageType"]);
        Assert.Equal("c:/", result["KernelMemory__Services__SimpleVectorDb__Directory"]);
    }

    [Fact]
    public void ItWorksForSimpleQueuesConfig()
    {
        // Arrange
        var target = new SimpleQueuesConfig
        {
            StorageType = FileSystemTypes.Disk,
            Directory = "c:/",
            PollDelayMsecs = 1,
            DispatchFrequencyMsecs = 2,
            FetchBatchSize = 3,
            FetchLockSeconds = 4,
            MaxRetriesBeforePoisonQueue = 5,
            PoisonQueueSuffix = "dl"
        };

        // Act
        Dictionary<string, string> result = ConfigEnvVars.GenerateEnvVarsFromObject(
            target, "KernelMemory", "Services", "SimpleQueues");
        foreach (var v in result) { Console.WriteLine($"{v.Key}: {v.Value}"); }

        // Assert
        Assert.Equal(8, result.Count);
        Assert.Equal("Disk", result["KernelMemory__Services__SimpleQueues__StorageType"]);
        Assert.Equal("c:/", result["KernelMemory__Services__SimpleQueues__Directory"]);
        Assert.Equal("1", result["KernelMemory__Services__SimpleQueues__PollDelayMsecs"]);
        Assert.Equal("2", result["KernelMemory__Services__SimpleQueues__DispatchFrequencyMsecs"]);
        Assert.Equal("3", result["KernelMemory__Services__SimpleQueues__FetchBatchSize"]);
        Assert.Equal("4", result["KernelMemory__Services__SimpleQueues__FetchLockSeconds"]);
        Assert.Equal("5", result["KernelMemory__Services__SimpleQueues__MaxRetriesBeforePoisonQueue"]);
        Assert.Equal("dl", result["KernelMemory__Services__SimpleQueues__PoisonQueueSuffix"]);
    }
}
