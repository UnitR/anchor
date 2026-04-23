using Anchor.Shared.Models;

namespace Anchor.Desktop.Views;

public sealed record IntentionReconciliation(bool OnTrack, bool Resume, string? Notes);

public partial class IntentionCheckpointPage : ContentPage
{
    private readonly TaskCompletionSource<IntentionReconciliation> _tcs = new();
    public Task<IntentionReconciliation> CompletionTask => _tcs.Task;

    private IntentionRecord? _intention;
    public IntentionRecord? Intention
    {
        get => _intention;
        set { _intention = value; if (value is not null) Render(value); }
    }

    public IntentionCheckpointPage() { InitializeComponent(); }

    private void Render(IntentionRecord r)
    {
        GoalLabel.Text = r.PrimaryGoal;
        IfThenLabel.Text = $"If {r.IfCondition} → then {r.ThenAction}";
    }

    private void OnTrack(object? sender, EventArgs e) =>
        _tcs.TrySetResult(new IntentionReconciliation(true, true, null));

    private void OnDrifted(object? sender, EventArgs e) => DriftedPanel.IsVisible = true;

    private void OnResume(object? sender, EventArgs e) =>
        _tcs.TrySetResult(new IntentionReconciliation(false, true, DriftReason.Text));

    private void OnDefer(object? sender, EventArgs e) =>
        _tcs.TrySetResult(new IntentionReconciliation(false, false, DriftReason.Text));
}
