using Anchor.Shared.Scheduling;
using Xunit;

namespace Anchor.Shared.Tests;

public class UltradianSchedulerTests
{
    [Fact]
    public void Next_Is_Within_Mean_Plus_Minus_Jitter()
    {
        var s = new UltradianScheduler(
            meanInterval: TimeSpan.FromMinutes(52),
            jitterHalfWidth: TimeSpan.FromMinutes(8),
            seed: 42);
        var from = DateTimeOffset.UtcNow;
        for (var i = 0; i < 500; i++)
        {
            var t = s.Next(from);
            var delta = t.At - from;
            Assert.True(delta >= TimeSpan.FromMinutes(44));
            Assert.True(delta <= TimeSpan.FromMinutes(60));
            Assert.Equal(TriggerReason.Ultradian, t.Reason);
        }
    }

    [Fact]
    public void Jitter_Must_Be_Less_Than_Mean()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UltradianScheduler(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10)));
    }
}
