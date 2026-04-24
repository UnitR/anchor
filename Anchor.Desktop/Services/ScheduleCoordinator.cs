using Anchor.Desktop.Views;
using Anchor.Shared.Models;
using Anchor.Shared.Scheduling;
using Anchor.Shared.Storage;
using Microsoft.Extensions.Logging;

namespace Anchor.Desktop.Services;

/// <summary>
/// Owns the session lifecycle: starts the ultradian+checkpoint scheduler, waits for the
/// next trigger, shows the interrupt overlay with the three-step flow
/// (interoception → intention reconciliation → anchor challenge), then reschedules.
/// </summary>
public sealed class ScheduleCoordinator
{
    private readonly IAnchorRepository _repo;
    private readonly IOverlayController _overlay;
    private readonly LocalPairingService _pairing;
    private readonly IServiceProvider _sp;
    private readonly ILogger<ScheduleCoordinator> _log;

    private CancellationTokenSource? _cts;
    private CheckpointScheduler? _scheduler;
    private IntentionRecord? _currentIntention;

    public ScheduleCoordinator(
        IAnchorRepository repo,
        IOverlayController overlay,
        LocalPairingService pairing,
        IServiceProvider sp,
        ILogger<ScheduleCoordinator> log)
    {
        _repo = repo;
        _overlay = overlay;
        _pairing = pairing;
        _sp = sp;
        _log = log;
    }

    public async Task StartSessionAsync(IntentionRecord intention)
    {
        _currentIntention = intention;
        await _repo.SaveIntentionAsync(intention);
        await _repo.LogEventAsync(new SessionEvent(Guid.NewGuid(), intention.SessionId, SessionEventKind.SessionStarted, DateTimeOffset.UtcNow, null));

        _scheduler = CheckpointScheduler.ForIntention(intention);
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(_cts.Token);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _currentIntention is not null && _scheduler is not null)
        {
            var next = _scheduler.ComputeNext(DateTimeOffset.UtcNow);
            var delay = next.At - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                try { await Task.Delay(delay, ct); }
                catch (TaskCanceledException) { return; }
            }
            if (ct.IsCancellationRequested) return;
            if (next.Reason == TriggerReason.Checkpoint) _scheduler.MarkCheckpointFired(next.At);
            await FireInterruptAsync(next);
        }
    }

    private async Task FireInterruptAsync(InterruptTrigger trigger)
    {
        if (_currentIntention is null) return;

        await _repo.LogEventAsync(new SessionEvent(Guid.NewGuid(), _currentIntention.SessionId,
            SessionEventKind.InterruptTriggered, DateTimeOffset.UtcNow, $"reason={trigger.Reason}"));

        // Step 1 — Interoception
        var interoceptionPage = _sp.GetRequiredService<InteroceptionPromptPage>();
        await _overlay.ShowAsync(interoceptionPage);
        var interoception = await interoceptionPage.CompletionTask;
        await _repo.AddInteroceptionAsync(interoception);
        await _repo.LogEventAsync(new SessionEvent(Guid.NewGuid(), _currentIntention.SessionId,
            SessionEventKind.InteroceptionAnswered, DateTimeOffset.UtcNow, null));

        // Step 2 — Intention reconciliation
        var checkpointPage = _sp.GetRequiredService<IntentionCheckpointPage>();
        checkpointPage.Intention = _currentIntention;
        await _overlay.ShowAsync(checkpointPage);
        var reconciliation = await checkpointPage.CompletionTask;
        await _repo.LogEventAsync(new SessionEvent(Guid.NewGuid(), _currentIntention.SessionId,
            reconciliation.OnTrack ? SessionEventKind.IntentionOnTrack : SessionEventKind.IntentionDrifted,
            DateTimeOffset.UtcNow, reconciliation.Notes));

        // Step 3 — Anchor challenge
        var anchors = await _repo.ListAnchorsAsync();
        if (anchors.Count == 0)
        {
            _log.LogWarning("No anchor objects registered — the interruption cannot be completed. Skipping the physical-anchor gate for this cycle.");
            await _overlay.DismissAsync();
            return;
        }
        var chosen = anchors[Random.Shared.Next(anchors.Count)];
        var challengePage = _sp.GetRequiredService<AnchorChallengePage>();
        challengePage.Anchor = chosen;
        await _pairing.IssueChallengeAsync(chosen);
        await _overlay.ShowAsync(challengePage);
        var verdict = await challengePage.CompletionTask;

        await _repo.LogEventAsync(new SessionEvent(Guid.NewGuid(), _currentIntention.SessionId,
            verdict.Passed ? SessionEventKind.AnchorChallengePassed : SessionEventKind.AnchorChallengeFailed,
            DateTimeOffset.UtcNow, verdict.FailReason?.ToString()));

        if (verdict.Passed)
        {
            await _overlay.DismissAsync();
        }
        // If failed, loop: the overlay stays up and the phone keeps retrying until pass or emergency bypass.
    }

    public async Task EmergencyBypassAsync()
    {
        if (_currentIntention is null) return;
        await _repo.LogEventAsync(new SessionEvent(Guid.NewGuid(), _currentIntention.SessionId,
            SessionEventKind.EmergencyBypassUsed, DateTimeOffset.UtcNow, null));
        // Mandatory 60s cooldown before the overlay releases input.
        await Task.Delay(TimeSpan.FromSeconds(60));
        await _overlay.DismissAsync();
    }

    public async Task EndSessionAsync()
    {
        _cts?.Cancel();
        if (_currentIntention is not null)
        {
            await _repo.LogEventAsync(new SessionEvent(Guid.NewGuid(), _currentIntention.SessionId,
                SessionEventKind.SessionEnded, DateTimeOffset.UtcNow, null));
        }
        _currentIntention = null;
    }
}
