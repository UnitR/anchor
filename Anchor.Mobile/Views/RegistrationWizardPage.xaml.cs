using Anchor.Mobile.Services;
using Anchor.Shared.Models;
using Anchor.Shared.Storage;
using UIKit;

namespace Anchor.Mobile.Views;

public partial class RegistrationWizardPage : ContentPage
{
    private readonly VisionValidator _vision;
    private readonly IAnchorRepository _repo;
    private readonly List<float[]> _prints = new();
    private readonly List<string> _accumulatedClasses = new();

    public RegistrationWizardPage(VisionValidator vision, IAnchorRepository repo)
    {
        InitializeComponent();
        _vision = vision;
        _repo = repo;
    }

    private async void OnCapture(object? s, EventArgs e)
    {
        var photo = await MediaPicker.CapturePhotoAsync();
        if (photo is null) return;
        using var stream = await photo.OpenReadAsync();
        var data = Foundation.NSData.FromStream(stream)!;
        var img = UIImage.LoadFromData(data)!;
        var v = await _vision.AnalyzeAsync(img);
        _prints.Add(v.FeaturePrintVector);
        foreach (var c in v.TopClasses)
            if (!_accumulatedClasses.Contains(c)) _accumulatedClasses.Add(c);
        ShotCount.Text = $"{_prints.Count} / 3 reference shots captured.";
        SaveBtn.IsEnabled = _prints.Count >= 3;
    }

    private async void OnSave(object? s, EventArgs e)
    {
        var obj = new AnchorObject(
            Guid.NewGuid(),
            NameEntry.Text ?? "object",
            RoomEntry.Text ?? "room",
            _prints.ToArray(),
            _accumulatedClasses.Take(5).ToArray(),
            DateTimeOffset.UtcNow);
        await _repo.UpsertAnchorObjectAsync(obj);
        await DisplayAlertAsync("Saved", $"{obj.Name} registered in {obj.Room}.", "OK");
        await Shell.Current.Navigation.PopAsync();
    }
}
