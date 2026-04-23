using Anchor.Shared.Models;

namespace Anchor.Desktop.Views;

public partial class InteroceptionPromptPage : ContentPage
{
    private readonly TaskCompletionSource<InteroceptionAnswer> _tcs = new();
    public Task<InteroceptionAnswer> CompletionTask => _tcs.Task;

    public Guid SessionId { get; set; }

    private InteroceptionBand? _water, _food, _bathroom, _stood;

    public InteroceptionPromptPage()
    {
        InitializeComponent();
        BuildRow(WaterRow, v => { _water = v; Refresh(); });
        BuildRow(FoodRow, v => { _food = v; Refresh(); });
        BuildRow(BathroomRow, v => { _bathroom = v; Refresh(); });
        BuildRow(StoodRow, v => { _stood = v; Refresh(); });
    }

    private static readonly (InteroceptionBand band, string label)[] Bands =
    [
        (InteroceptionBand.Recent,   "< 1h"),
        (InteroceptionBand.Moderate, "1–3h"),
        (InteroceptionBand.Stale,    "3–6h"),
        (InteroceptionBand.Critical, "6h+")
    ];

    private void BuildRow(Layout host, Action<InteroceptionBand> onPick)
    {
        var buttons = new List<Button>();
        foreach (var (band, label) in Bands)
        {
            var btn = new Button { Text = label, Padding = 12 };
            btn.Clicked += (_, _) =>
            {
                foreach (var b in buttons) b.BackgroundColor = (Color)Resources["Surface"]!;
                btn.BackgroundColor = (Color)Resources["Accent"]!;
                onPick(band);
            };
            buttons.Add(btn);
            host.Add(btn);
        }
    }

    private void Refresh() =>
        ContinueBtn.IsEnabled = _water.HasValue && _food.HasValue && _bathroom.HasValue && _stood.HasValue;

    private void OnContinue(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(new InteroceptionAnswer(
            SessionId,
            DateTimeOffset.UtcNow,
            _water!.Value, _food!.Value, _bathroom!.Value, _stood!.Value));
    }
}
