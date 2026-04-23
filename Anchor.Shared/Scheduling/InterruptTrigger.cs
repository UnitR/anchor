namespace Anchor.Shared.Scheduling;

public enum TriggerReason { Ultradian, Checkpoint, Manual }

public sealed record InterruptTrigger(
    DateTimeOffset At,
    TriggerReason Reason,
    string? CheckpointLabel);
