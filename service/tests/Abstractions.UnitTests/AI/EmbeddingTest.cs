// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

namespace Microsoft.KM.Abstractions.UnitTests.AI;

public class EmbeddingTest
{
    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItShowsEmbeddingSizeWhenThrowing()
    {
        // Arrange
        var vec1 = new Embedding(new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0 });
        var vec2 = new Embedding(new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0, 0, 0 });

        // Act - Assert
        var e = Assert.Throws<InvalidOperationException>(() => vec1.CosineSimilarity(vec2));
        Assert.Contains(" 11", e.Message, StringComparison.Ordinal);
        Assert.Contains(" 13", e.Message, StringComparison.Ordinal);
    }
}
