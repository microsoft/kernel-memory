// Copyright (c) Microsoft. All rights reserved.

using System.Security.Cryptography;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.TestHelpers;
using Moq;
using Xunit.Abstractions;

namespace Core.UnitTests.MemoryStorage.DevTools;

public class SimpleVectorDbTest : BaseUnitTestCase
{
    public SimpleVectorDbTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public async Task ItSearchesAllRecords()
    {
        const string index = "test";
        const string text = "text";

        // Arrange
        var emb = new Mock<ITextEmbeddingGenerator>();
        emb.Setup(x => x.GenerateEmbeddingAsync(text, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Embedding(new[] { 0.5f, 0.5f, 0.5f }));

        var target = new SimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Volatile }, emb.Object);
        await target.CreateIndexAsync(index, 3);

        // Store 1000 records with random embeddings
        for (int i = 0; i < 1000; i++)
        {
            await target.UpsertAsync(index, new MemoryRecord
            {
                Id = RandomLetter() + RandomLetter() + RandomLetter() + $"_{i}",
                Vector = new Embedding(new[] { RandomNumber() / 1000f, RandomNumber() / 1000f, RandomNumber() / 1000f }),
            });
        }

        // Act - Search top 25 records, result is ordered by similarity
        const int count = 25;
        List<(MemoryRecord, double)> result1 = await target.GetSimilarListAsync(index, text, minRelevance: 0.3, limit: count).ToListAsync();

        // Act - Search all records, result is ordered by similarity
        List<(MemoryRecord, double)> result2 = await target.GetSimilarListAsync(index, text, minRelevance: 0.3, limit: -1).ToListAsync();

        // Assert - the two lists should have the same top 25
        for (int i = 0; i < count; i++)
        {
            Assert.Equal(result1[i].Item1.Id, result2[i].Item1.Id);
        }
    }

    private static string RandomLetter()
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] randomBytes = new byte[1];
        rng.GetBytes(randomBytes);

        // ASCII code for 'A' is 65, and for 'Z' is 90
        int randomAscii = (randomBytes[0] % 26) + 65;
        char randomLetter = (char)randomAscii;
        return $"{randomLetter}";
    }

    private static int RandomNumber()
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] randomBytes = new byte[4];
        rng.GetBytes(randomBytes);

        // Convert the 4 bytes to an integer
        int randomInt = BitConverter.ToInt32(randomBytes, 0);

        // Ensure the result is positive and within the range
        return Math.Abs(randomInt % 1000) + 1;
    }
}
