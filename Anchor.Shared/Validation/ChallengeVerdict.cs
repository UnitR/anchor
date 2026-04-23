using Anchor.Shared.Protocol;

namespace Anchor.Shared.Validation;

/// <summary>
/// Combined verdict from the three-gate anchor validation:
///   Gate 1: feature-print similarity ≥ threshold
///   Gate 2: Vision classification includes expected class
///   Gate 3: Foundation Models scene reasoning confirms room
/// Additionally the motion-freshness check must have passed.
/// All four must be true for Passed. The first failure captured drives the fail reason.
/// </summary>
public sealed record ChallengeVerdict(
    bool Passed,
    float FeaturePrintSimilarity,
    bool ClassificationMatched,
    bool SceneMatched,
    bool MotionFresh,
    float AggregateConfidence,
    ChallengeFailReason? FailReason)
{
    public static ChallengeVerdict FromGates(
        float similarity,
        float threshold,
        bool classificationMatched,
        bool sceneMatched,
        bool motionFresh)
    {
        ChallengeFailReason? reason = null;
        if (!motionFresh) reason = ChallengeFailReason.MotionFreshnessFailed;
        else if (similarity < threshold) reason = ChallengeFailReason.ObjectMismatch;
        else if (!classificationMatched) reason = ChallengeFailReason.ObjectMismatch;
        else if (!sceneMatched) reason = ChallengeFailReason.SceneMismatch;

        var passed = reason is null;
        // Aggregate confidence: average the booleans (as 1/0) and similarity, weighted.
        var boolAvg = ((classificationMatched ? 1f : 0f) + (sceneMatched ? 1f : 0f) + (motionFresh ? 1f : 0f)) / 3f;
        var aggregate = (similarity * 0.5f) + (boolAvg * 0.5f);

        return new ChallengeVerdict(passed, similarity, classificationMatched, sceneMatched, motionFresh, aggregate, reason);
    }
}
