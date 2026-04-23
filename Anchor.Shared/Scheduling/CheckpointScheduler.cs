using Anchor.Shared.Models;

namespace Anchor.Shared.Scheduling;

/// <summary>
/// Merges user-defined implementation-intention checkpoints with the ultradian
/// timer. Whichever is sooner wins. A checkpoint fired cannot be re-fired.
/// </summary>
public sealed class CheckpointScheduler
{
    private readonly UltradianScheduler _ultradian;
    private readonly SortedSet<DateTimeOffset> _pendingCheckpoints;

    public CheckpointScheduler(UltradianScheduler ultradian, IEnumerable<DateTimeOffset> checkpoints)
    {
        _ultradian = ultradian;
        _pendingCheckpoints = new SortedSet<DateTimeOffset>(checkpoints);
    }

    public static CheckpointScheduler ForIntention(IntentionRecord record, UltradianScheduler? ultradian = null)
        => new(ultradian ?? new UltradianScheduler(), record.ExplicitCheckpoints);

    public InterruptTrigger ComputeNext(DateTimeOffset from)
    {
        var ultradianTrigger = _ultradian.Next(from);
        if (_pendingCheckpoints.Count == 0) return ultradianTrigger;

        var nextCheckpoint = _pendingCheckpoints.Min;
        return nextCheckpoint < ultradianTrigger.At
            ? new InterruptTrigger(nextCheckpoint, TriggerReason.Checkpoint, nextCheckpoint.ToString("HH:mm"))
            : ultradianTrigger;
    }

    public void MarkCheckpointFired(DateTimeOffset at) => _pendingCheckpoints.Remove(at);
}
