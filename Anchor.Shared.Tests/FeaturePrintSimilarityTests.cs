using Anchor.Shared.Validation;
using Xunit;

namespace Anchor.Shared.Tests;

public class FeaturePrintSimilarityTests
{
    [Fact]
    public void Identical_Vectors_Score_One()
    {
        var a = new float[] { 1, 2, 3, 4 };
        Assert.Equal(1f, FeaturePrintSimilarity.CosineSimilarity(a, a), 5);
    }

    [Fact]
    public void Orthogonal_Vectors_Score_Zero()
    {
        var a = new float[] { 1, 0 };
        var b = new float[] { 0, 1 };
        Assert.Equal(0f, FeaturePrintSimilarity.CosineSimilarity(a, b), 5);
    }

    [Fact]
    public void BestMatch_Picks_Highest()
    {
        var candidate = new float[] { 1, 0, 0 };
        var refs = new List<float[]>
        {
            new float[] { 0, 1, 0 },
            new float[] { 0.9f, 0.1f, 0 },
            new float[] { 0, 0, 1 },
        };
        var best = FeaturePrintSimilarity.BestMatch(candidate, refs);
        Assert.InRange(best, 0.99f, 1.001f);
    }

    [Fact]
    public void Length_Mismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            FeaturePrintSimilarity.CosineSimilarity(new float[] { 1, 2 }, new float[] { 1, 2, 3 }));
    }
}
