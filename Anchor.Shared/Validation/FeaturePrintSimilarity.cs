namespace Anchor.Shared.Validation;

/// <summary>
/// Cosine similarity helper for Vision feature-print vectors. Runs on either side
/// (iOS computes the print; either device can compare). Used as one of three gates
/// in the anchor-object challenge.
/// </summary>
public static class FeaturePrintSimilarity
{
    /// <summary>Default threshold calibrated empirically for VNGenerateImageFeaturePrintRequest.</summary>
    public const float DefaultMatchThreshold = 0.82f;

    public static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Feature-print vectors must have identical length.");
        float dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        if (normA == 0 || normB == 0) return 0f;
        return (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
    }

    /// <summary>Returns the best (max) similarity against any reference print for the object.</summary>
    public static float BestMatch(ReadOnlySpan<float> candidate, IReadOnlyList<float[]> references)
    {
        var best = 0f;
        foreach (var r in references)
        {
            var s = CosineSimilarity(candidate, r);
            if (s > best) best = s;
        }
        return best;
    }
}
