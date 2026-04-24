using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Anchor.Shared.Protocol;
using Anchor.Shared.Storage;

namespace Anchor.Mobile.Services;

/// <summary>
/// iPhone side of the pairing. Connects to the desktop's WebSocket, listens for
/// ChallengeIssued, opens the camera, and sends ChallengePassed (HMAC-signed) or
/// ChallengeFailed back.
/// </summary>
public sealed class DesktopConnection
{
    private readonly IAnchorRepository _repo;
    private ClientWebSocket? _ws;
    public event Func<ChallengeIssued, Task>? OnChallenge;

    public DesktopConnection(IAnchorRepository repo) { _repo = repo; }

    public async Task ConnectAsync(string desktopHost, CancellationToken ct = default)
    {
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(new Uri($"ws://{desktopHost}:{LocalPairingServicePort}/anchor/"), ct);
        _ = Task.Run(() => ReceiveAsync(ct), ct);
    }

    private const int LocalPairingServicePort = 43987;

    private async Task ReceiveAsync(CancellationToken ct)
    {
        var buf = new byte[64 * 1024];
        while (_ws!.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var ms = new MemoryStream();
            WebSocketReceiveResult r;
            do { r = await _ws.ReceiveAsync(buf, ct); ms.Write(buf, 0, r.Count); } while (!r.EndOfMessage);
            if (r.MessageType == WebSocketMessageType.Close) return;
            var json = Encoding.UTF8.GetString(ms.ToArray());
            if (JsonSerializer.Deserialize<WireMessage>(json) is ChallengeIssued ci && OnChallenge is not null)
                await OnChallenge.Invoke(ci);
        }
    }

    public async Task SendPassAsync(Guid challengeId, float confidence)
    {
        var secret = await _repo.GetPairingSecretAsync() ?? throw new InvalidOperationException("Not paired.");
        var payload = $"{challengeId}:{confidence:F4}";
        var token = HmacPairing.Sign(secret, payload);
        await SendAsync(new ChallengePassed(Guid.NewGuid(), DateTimeOffset.UtcNow, challengeId, token, confidence));
    }

    public Task SendFailAsync(Guid challengeId, ChallengeFailReason reason, string? hint) =>
        SendAsync(new ChallengeFailed(Guid.NewGuid(), DateTimeOffset.UtcNow, challengeId, reason, hint));

    private async Task SendAsync(WireMessage msg)
    {
        if (_ws is null || _ws.State != WebSocketState.Open) return;
        var json = JsonSerializer.Serialize<WireMessage>(msg);
        await _ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
