using System.Text.Json.Serialization;

namespace Anchor.Shared.Protocol;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ChallengeIssued), "challenge_issued")]
[JsonDerivedType(typeof(ChallengePassed), "challenge_passed")]
[JsonDerivedType(typeof(ChallengeFailed), "challenge_failed")]
[JsonDerivedType(typeof(PairingRequest), "pair_request")]
[JsonDerivedType(typeof(PairingConfirm), "pair_confirm")]
[JsonDerivedType(typeof(Heartbeat), "heartbeat")]
public abstract record WireMessage(Guid MessageId, DateTimeOffset SentAt);

public sealed record ChallengeIssued(
    Guid MessageId,
    DateTimeOffset SentAt,
    Guid ChallengeId,
    string AnchorObjectName,
    string ExpectedRoom,
    Guid AnchorObjectId) : WireMessage(MessageId, SentAt);

public sealed record ChallengePassed(
    Guid MessageId,
    DateTimeOffset SentAt,
    Guid ChallengeId,
    string SignedToken,
    float Confidence) : WireMessage(MessageId, SentAt);

public enum ChallengeFailReason
{
    ObjectMismatch,
    SceneMismatch,
    MotionFreshnessFailed,
    StaleCapture,
    LowConfidence,
    Cancelled
}

public sealed record ChallengeFailed(
    Guid MessageId,
    DateTimeOffset SentAt,
    Guid ChallengeId,
    ChallengeFailReason Reason,
    string? DebugHint) : WireMessage(MessageId, SentAt);

public sealed record PairingRequest(
    Guid MessageId,
    DateTimeOffset SentAt,
    string DeviceName,
    string DevicePublicKeyBase64) : WireMessage(MessageId, SentAt);

public sealed record PairingConfirm(
    Guid MessageId,
    DateTimeOffset SentAt,
    string SharedSecretBase64) : WireMessage(MessageId, SentAt);

public sealed record Heartbeat(
    Guid MessageId,
    DateTimeOffset SentAt) : WireMessage(MessageId, SentAt);
