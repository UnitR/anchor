using Anchor.Shared.Protocol;
using Xunit;

namespace Anchor.Shared.Tests;

public class HmacPairingTests
{
    [Fact]
    public void Sign_Then_Verify_Succeeds()
    {
        var secret = HmacPairing.NewSharedSecret();
        Assert.Equal(32, secret.Length);
        var sig = HmacPairing.Sign(secret, "hello-challenge-token");
        Assert.True(HmacPairing.Verify(secret, "hello-challenge-token", sig));
    }

    [Fact]
    public void Verify_Rejects_Tampered_Payload()
    {
        var secret = HmacPairing.NewSharedSecret();
        var sig = HmacPairing.Sign(secret, "pass");
        Assert.False(HmacPairing.Verify(secret, "PASS", sig));
    }

    [Fact]
    public void Verify_Rejects_Wrong_Secret()
    {
        var s1 = HmacPairing.NewSharedSecret();
        var s2 = HmacPairing.NewSharedSecret();
        var sig = HmacPairing.Sign(s1, "x");
        Assert.False(HmacPairing.Verify(s2, "x", sig));
    }
}
