namespace Anchor.Shared.Scheduling;

/// <summary>
/// Computes next ultradian interrupt based on Kleitman's Basic Rest-Activity Cycle.
/// Default 52min mean with ±8min uniform jitter. Jitter prevents user from
/// gaming the schedule (a predictable timer becomes ignorable).
/// </summary>
public sealed class UltradianScheduler
{
    public TimeSpan MeanInterval { get; }
    public TimeSpan JitterHalfWidth { get; }
    private readonly Random _rng;

    public UltradianScheduler(
        TimeSpan? meanInterval = null,
        TimeSpan? jitterHalfWidth = null,
        int? seed = null)
    {
        MeanInterval = meanInterval ?? TimeSpan.FromMinutes(52);
        JitterHalfWidth = jitterHalfWidth ?? TimeSpan.FromMinutes(8);
        if (JitterHalfWidth >= MeanInterval)
            throw new ArgumentOutOfRangeException(nameof(jitterHalfWidth), "Jitter must be strictly less than mean.");
        _rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
    }

    public InterruptTrigger Next(DateTimeOffset from)
    {
        var jitterRange = JitterHalfWidth.TotalSeconds * 2;
        var offsetSeconds = (_rng.NextDouble() * jitterRange) - JitterHalfWidth.TotalSeconds;
        var at = from + MeanInterval + TimeSpan.FromSeconds(offsetSeconds);
        return new InterruptTrigger(at, TriggerReason.Ultradian, null);
    }
}
