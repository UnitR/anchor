using Anchor.Shared.Protocol;
using Anchor.Shared.Validation;
using Xunit;

namespace Anchor.Shared.Tests;

public class ChallengeVerdictTests
{
    [Fact]
    public void All_Gates_Pass_Yields_Passed()
    {
        var v = ChallengeVerdict.FromGates(0.95f, 0.82f, true, true, true);
        Assert.True(v.Passed);
        Assert.Null(v.FailReason);
    }

    [Fact]
    public void Motion_Failure_Short_Circuits()
    {
        var v = ChallengeVerdict.FromGates(0.95f, 0.82f, true, true, motionFresh: false);
        Assert.False(v.Passed);
        Assert.Equal(ChallengeFailReason.MotionFreshnessFailed, v.FailReason);
    }

    [Fact]
    public void Low_Similarity_Fails_With_ObjectMismatch()
    {
        var v = ChallengeVerdict.FromGates(0.5f, 0.82f, true, true, true);
        Assert.Equal(ChallengeFailReason.ObjectMismatch, v.FailReason);
    }

    [Fact]
    public void Scene_Mismatch_Fails_With_SceneMismatch()
    {
        var v = ChallengeVerdict.FromGates(0.95f, 0.82f, classificationMatched: true, sceneMatched: false, motionFresh: true);
        Assert.Equal(ChallengeFailReason.SceneMismatch, v.FailReason);
    }
}
