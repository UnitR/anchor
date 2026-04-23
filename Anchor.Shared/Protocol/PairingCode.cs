using System.Security.Cryptography;

namespace Anchor.Shared.Protocol;

/// <summary>
/// 6-digit code shown on desktop; user types it into phone.
/// Short-lived (2 min) to minimize interception risk on LAN.
/// </summary>
public sealed record PairingCode(string Code, DateTimeOffset ExpiresAt)
{
    public static PairingCode Generate(TimeSpan? lifetime = null)
    {
        Span<byte> buf = stackalloc byte[4];
        RandomNumberGenerator.Fill(buf);
        var n = BitConverter.ToUInt32(buf) % 1_000_000u;
        return new PairingCode(n.ToString("D6"), DateTimeOffset.UtcNow + (lifetime ?? TimeSpan.FromMinutes(2)));
    }

    public bool IsValidAt(DateTimeOffset now) => now < ExpiresAt;
}
