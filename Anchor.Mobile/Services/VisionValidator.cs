using Foundation;
using ImageIO;
using UIKit;
using Vision;

namespace Anchor.Mobile.Services;

/// <summary>
/// On-device object validation using the Vision framework. No network, no cloud.
/// Produces a feature-print vector and the top-N classification labels, which the
/// caller combines with a Foundation Models scene-reasoning pass.
/// </summary>
public sealed class VisionValidator
{
    public sealed record VisionResult(float[] FeaturePrintVector, IReadOnlyList<string> TopClasses);

    public async Task<VisionResult> AnalyzeAsync(UIImage image)
    {
        var fp = await FeaturePrintAsync(image);
        var classes = await ClassifyAsync(image);
        return new VisionResult(fp, classes);
    }

    private static Task<float[]> FeaturePrintAsync(UIImage image)
    {
        var tcs = new TaskCompletionSource<float[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        var request = new VNGenerateImageFeaturePrintRequest((req, err) =>
        {
            if (err is not null) { tcs.TrySetException(new Exception(err.LocalizedDescription)); return; }
            if (req.GetResults<VNFeaturePrintObservation>() is { Length: > 0 } observations)
            {
                var obs = observations[0];
                var data = obs.Data;
                // Feature prints are Float32 by default.
                var count = (int)(data.Length / sizeof(float));
                var vec = new float[count];
                System.Runtime.InteropServices.Marshal.Copy(data.Bytes, vec, 0, count);
                tcs.TrySetResult(vec);
            }
            else tcs.TrySetResult(Array.Empty<float>());
        })
        { ImageCropAndScaleOption = VNImageCropAndScaleOption.CenterCrop };

        var handler = new VNImageRequestHandler(image.CGImage!, CGImagePropertyOrientation.Up, new NSDictionary());
        _ = Task.Run(() =>
        {
            try { handler.Perform(new[] { request }, out var perfErr); if (perfErr is not null) tcs.TrySetException(new Exception(perfErr.LocalizedDescription)); }
            catch (Exception ex) { tcs.TrySetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<IReadOnlyList<string>> ClassifyAsync(UIImage image)
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<string>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var request = new VNClassifyImageRequest((req, err) =>
        {
            if (err is not null) { tcs.TrySetException(new Exception(err.LocalizedDescription)); return; }
            var observations = req.GetResults<VNClassificationObservation>() ?? Array.Empty<VNClassificationObservation>();
            var names = observations
                .Where(o => o.Confidence >= 0.3f)
                .OrderByDescending(o => o.Confidence)
                .Take(8)
                .Select(o => o.Identifier)
                .ToList();
            tcs.TrySetResult(names);
        });

        var handler = new VNImageRequestHandler(image.CGImage!, CGImagePropertyOrientation.Up, new NSDictionary());
        _ = Task.Run(() =>
        {
            try { handler.Perform(new[] { request }, out var perfErr); if (perfErr is not null) tcs.TrySetException(new Exception(perfErr.LocalizedDescription)); }
            catch (Exception ex) { tcs.TrySetException(ex); }
        });
        return tcs.Task;
    }
}
