namespace Anchor.Shared.Models;

public enum SessionEventKind
{
    SessionStarted,
    InterruptTriggered,
    InteroceptionAnswered,
    IntentionOnTrack,
    IntentionDrifted,
    AnchorChallengeIssued,
    AnchorChallengePassed,
    AnchorChallengeFailed,
    EmergencyBypassUsed,
    SessionEnded
}

public sealed record SessionEvent(
    Guid Id,
    Guid SessionId,
    SessionEventKind Kind,
    DateTimeOffset At,
    string? DetailsJson);
