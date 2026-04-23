using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Anchor.Shared.Models;
using Anchor.Shared.Protocol;
using Anchor.Shared.Storage;
using Anchor.Shared.Validation;
using Microsoft.Extensions.Logging;

namespace Anchor.Desktop.Services;

/// <summary>
/// Local-network WebSocket server. Advertises over mDNS (future: bonjour via
/// platform-specific code) and negotiates a one-time pairing code with the phone.
/// After pairing, every ChallengePassed from the phone is HMAC-verified before we
/// dismiss the overlay.
/// </summary>
public sealed class LocalPairingService
{
    public const int Port = 43987;

    private readonly IAnchorRepository _repo;
    private readonly ILogger<LocalPairingService> _log;
    private HttpListener? _listener;
    private WebSocket? _phoneSocket;
    private TaskCompletionSource<ChallengeVerdict>? _pendingChallenge;
    private Guid _pendingChallengeId;

    public LocalPairingService(IAnchorRepository repo, ILogger<LocalPairingService> log)
    {
        _repo = repo;
        _log = log;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{Port}/anchor/");
        _listener.Start();
        _ = Task.Run(() => AcceptLoopAsync(ct), ct);
        return Task.CompletedTask;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (_listener is not null && !ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch (HttpListenerException) { return; }
            if (!ctx.Request.IsWebSocketRequest) { ctx.Response.StatusCode = 400; ctx.Response.Close(); continue; }
            var wsCtx = await ctx.AcceptWebSocketAsync(subProtocol: null);
            _phoneSocket = wsCtx.WebSocket;
            _ = Task.Run(() => ReceiveLoopAsync(_phoneSocket, ct), ct);
        }
    }

    private async Task ReceiveLoopAsync(WebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var total = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(buffer, ct);
                total.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close) return;

            var payload = Encoding.UTF8.GetString(total.ToArray());
            try
            {
                var msg = JsonSerializer.Deserialize<WireMessage>(payload);
                await HandleMessageAsync(msg);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Bad wire message received from phone");
            }
        }
    }

    private async Task HandleMessageAsync(WireMessage? msg)
    {
        switch (msg)
        {
            case ChallengePassed p when p.ChallengeId == _pendingChallengeId:
                if (await VerifyTokenAsync(p))
                {
                    _pendingChallenge?.TrySetResult(new ChallengeVerdict(
                        Passed: true,
                        FeaturePrintSimilarity: p.Confidence,
                        ClassificationMatched: true,
                        SceneMatched: true,
                        MotionFresh: true,
                        AggregateConfidence: p.Confidence,
                        FailReason: null));
                }
                break;
            case ChallengeFailed f when f.ChallengeId == _pendingChallengeId:
                _pendingChallenge?.TrySetResult(ChallengeVerdict.FromGates(
                    similarity: 0, threshold: FeaturePrintSimilarity.DefaultMatchThreshold,
                    classificationMatched: f.Reason != ChallengeFailReason.ObjectMismatch,
                    sceneMatched: f.Reason != ChallengeFailReason.SceneMismatch,
                    motionFresh: f.Reason != ChallengeFailReason.MotionFreshnessFailed));
                break;
            case Heartbeat:
                // ignore
                break;
        }
    }

    private async Task<bool> VerifyTokenAsync(ChallengePassed p)
    {
        var secret = await _repo.GetPairingSecretAsync();
        if (secret is null) return false;
        var expectedPayload = $"{p.ChallengeId}:{p.Confidence:F4}";
        return HmacPairing.Verify(secret, expectedPayload, p.SignedToken);
    }

    public async Task<ChallengeVerdict> IssueChallengeAsync(AnchorObject anchor)
    {
        _pendingChallengeId = Guid.NewGuid();
        _pendingChallenge = new TaskCompletionSource<ChallengeVerdict>(TaskCreationOptions.RunContinuationsAsynchronously);
        var msg = new ChallengeIssued(
            Guid.NewGuid(), DateTimeOffset.UtcNow,
            _pendingChallengeId, anchor.Name, anchor.Room, anchor.Id);
        await SendAsync(msg);
        return await _pendingChallenge.Task;
    }

    private async Task SendAsync(WireMessage msg)
    {
        if (_phoneSocket is null || _phoneSocket.State != WebSocketState.Open) return;
        var json = JsonSerializer.Serialize<WireMessage>(msg);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _phoneSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task<PairingCode> BeginPairingAsync()
    {
        var code = PairingCode.Generate();
        var secret = HmacPairing.NewSharedSecret();
        await _repo.SetPairingSecretAsync(secret);
        // Code + secret are displayed/transferred during the pairing wizard on first run.
        return code;
    }
}
