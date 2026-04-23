#if IOS
using Anchor.iOS.Services;
using Anchor.Shared.Models;
using Anchor.Shared.Protocol;
using Anchor.Shared.Storage;
using Anchor.Shared.Validation;
using UIKit;

namespace Anchor.iOS.Views;

public partial class ChallengeCameraPage : ContentPage
{
    private readonly VisionValidator _vision;
    private readonly FoundationModelsClient _fm;
    private readonly MotionFreshnessCheck _motion;
    private readonly DesktopConnection _conn;
    private readonly IAnchorRepository _repo;

    private AnchorObject? _anchor;
    private Guid _challengeId;

    public ChallengeCameraPage(VisionValidator vision, FoundationModelsClient fm,
        MotionFreshnessCheck motion, DesktopConnection conn, IAnchorRepository repo)
    {
        InitializeComponent();
        _vision = vision; _fm = fm; _motion = motion; _conn = conn; _repo = repo;
    }

    public async Task PresentChallengeAsync(ChallengeIssued ci)
    {
        _challengeId = ci.ChallengeId;
        _anchor = await _repo.GetAnchorAsync(ci.AnchorObjectId);
        PromptLabel.Text = $"Photograph the {ci.AnchorObjectName} in the {ci.ExpectedRoom}";
    }

    private async void OnCapture(object? s, EventArgs e)
    {
        if (_anchor is null) { VerdictLabel.Text = "No active challenge."; return; }

        var motionTask = _motion.CheckAsync();

        var photo = await MediaPicker.CapturePhotoAsync();
        if (photo is null) return;
        using var stream = await photo.OpenReadAsync();
        var data = Foundation.NSData.FromStream(stream)!;
        var img = UIImage.LoadFromData(data)!;

        var vResult = await _vision.AnalyzeAsync(img);
        var similarity = FeaturePrintSimilarity.BestMatch(vResult.FeaturePrintVector, _anchor.ReferenceFeaturePrints);
        var classMatch = _anchor.ExpectedVisionClasses.Any(c => vResult.TopClasses.Contains(c, StringComparer.OrdinalIgnoreCase));

        var sceneVerdict = await _fm.AnalyzeSceneAsync(vResult.TopClasses, _anchor.Name, _anchor.Room);
        var motionFresh = await motionTask;

        var verdict = ChallengeVerdict.FromGates(
            similarity, FeaturePrintSimilarity.DefaultMatchThreshold,
            classMatch, sceneVerdict.Match, motionFresh);

        VerdictLabel.Text = verdict.Passed
            ? $"Accepted ({verdict.AggregateConfidence:P0})."
            : $"Rejected: {verdict.FailReason}.";

        if (verdict.Passed)
            await _conn.SendPassAsync(_challengeId, verdict.AggregateConfidence);
        else
            await _conn.SendFailAsync(_challengeId, verdict.FailReason!.Value, sceneVerdict.Reason);
    }
}
#else
namespace Anchor.iOS.Views;
public partial class ChallengeCameraPage : ContentPage
{
    public ChallengeCameraPage() { InitializeComponent(); }
    private void OnCapture(object? s, EventArgs e) { }
}
#endif
