using System.Security.Cryptography;
using System.Text;

namespace Anchor.Shared.Protocol;

/// <summary>
/// After initial 6-digit code pairing, desktop + phone exchange a 32-byte shared secret.
/// Every subsequent message is HMAC-SHA256 signed to prevent another device on the LAN
/// from forging challenge-passed tokens and dismissing the overlay.
/// </summary>
public static class HmacPairing
{
    public const int SecretLengthBytes = 32;

    public static byte[] NewSharedSecret() => RandomNumberGenerator.GetBytes(SecretLengthBytes);

    public static string Sign(byte[] sharedSecret, string payload)
    {
        using var hmac = new HMACSHA256(sharedSecret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    public static bool Verify(byte[] sharedSecret, string payload, string signatureBase64)
    {
        var expected = Sign(sharedSecret, payload);
        // Constant-time comparison:
        var a = Encoding.ASCII.GetBytes(expected);
        var b = Encoding.ASCII.GetBytes(signatureBase64);
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
