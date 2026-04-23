#if IOS
using CoreMotion;

namespace Anchor.iOS.Services;

/// <summary>
/// Freshness gate — proves the phone was physically moved recently, defeating the
/// attempt to hold the phone still and take a picture of a still photo on a screen.
/// Samples accelerometer magnitude for a short window and passes if variance exceeds
/// a small empirical threshold.
/// </summary>
public sealed class MotionFreshnessCheck
{
    private readonly CMMotionManager _mgr = new();

    public async Task<bool> CheckAsync(TimeSpan? window = null, float threshold = 0.05f)
    {
        window ??= TimeSpan.FromSeconds(3);
        if (!_mgr.AccelerometerAvailable) return true; // cannot enforce, permissive
        _mgr.AccelerometerUpdateInterval = 0.05;
        _mgr.StartAccelerometerUpdates();
        try
        {
            var start = DateTime.UtcNow;
            var samples = new List<double>();
            while (DateTime.UtcNow - start < window)
            {
                var d = _mgr.AccelerometerData;
                if (d is not null)
                {
                    var m = Math.Sqrt(d.Acceleration.X * d.Acceleration.X +
                                      d.Acceleration.Y * d.Acceleration.Y +
                                      d.Acceleration.Z * d.Acceleration.Z);
                    samples.Add(m);
                }
                await Task.Delay(50);
            }
            if (samples.Count < 4) return false;
            var avg = samples.Average();
            var variance = samples.Sum(v => (v - avg) * (v - avg)) / samples.Count;
            return variance >= threshold;
        }
        finally
        {
            _mgr.StopAccelerometerUpdates();
        }
    }
}
#endif
