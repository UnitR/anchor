using Anchor.Desktop.Services;
using Anchor.Shared.Models;
using Anchor.Shared.Validation;

namespace Anchor.Desktop.Views;

public partial class AnchorChallengePage : ContentPage
{
    private readonly TaskCompletionSource<ChallengeVerdict> _tcs = new();
    private readonly ScheduleCoordinator _coordinator;
    public Task<ChallengeVerdict> CompletionTask => _tcs.Task;

    private AnchorObject? _anchor;
    public AnchorObject? Anchor
    {
        get => _anchor;
        set { _anchor = value; if (value is not null) Render(value); }
    }

    public AnchorChallengePage(ScheduleCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
    }

    private void Render(AnchorObject a)
    {
        ObjectLabel.Text = a.Name;
        RoomLabel.Text = $"in the {a.Room}";
    }

    public void ReportVerdict(ChallengeVerdict v)
    {
        if (v.Passed) _tcs.TrySetResult(v);
        else StatusLabel.Text = $"Not accepted ({v.FailReason}). Try again from the correct room.";
    }

    private async void OnEmergency(object? sender, EventArgs e)
    {
        var ok = await DisplayAlertAsync(
            "Emergency bypass",
            "Only use this for genuine emergencies. A 60-second cooldown will begin; the app will log the bypass.",
            "Use bypass", "Cancel");
        if (!ok) return;
        await _coordinator.EmergencyBypassAsync();
        _tcs.TrySetResult(new ChallengeVerdict(true, 0, false, false, false, 0, null));
    }
}