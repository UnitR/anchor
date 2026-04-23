namespace Anchor.Shared.Models;

/// <summary>
/// Implementation-intention record captured at session start.
/// Structured to match Gollwitzer's if-then schema which drives the effect size.
/// </summary>
public sealed record IntentionRecord(
    Guid SessionId,
    string PrimaryGoal,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpectedEndAt,
    string IfCondition,
    string ThenAction,
    IReadOnlyList<DateTimeOffset> ExplicitCheckpoints);
