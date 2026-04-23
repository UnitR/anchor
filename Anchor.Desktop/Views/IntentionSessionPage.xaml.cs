using Anchor.Desktop.Services;
using Anchor.Shared.Models;

namespace Anchor.Desktop.Views;

public partial class IntentionSessionPage : ContentPage
{
    private readonly ScheduleCoordinator _coordinator;

    public IntentionSessionPage(ScheduleCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        EndTimePicker.Time = DateTime.Now.AddHours(2).TimeOfDay;
    }

    private async void OnBegin(object? sender, EventArgs e)
    {
        var now = DateTimeOffset.Now;
        var endAt = new DateTimeOffset(now.Date + EndTimePicker.Time, now.Offset);
        if (endAt < now) endAt = endAt.AddDays(1);

        var record = new IntentionRecord(
            SessionId: Guid.NewGuid(),
            PrimaryGoal: GoalEntry.Text ?? "",
            StartedAt: now,
            ExpectedEndAt: endAt,
            IfCondition: IfEntry.Text ?? "",
            ThenAction: ThenEntry.Text ?? "",
            ExplicitCheckpoints: Array.Empty<DateTimeOffset>());

        await _coordinator.StartSessionAsync(record);
        await DisplayAlert("Session active",
            "Your desktop will be non-dismissibly interrupted on an ultradian schedule. Keep your phone within reach — that's how you clear the overlay.",
            "Got it");
    }
}
