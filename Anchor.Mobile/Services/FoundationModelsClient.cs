namespace Anchor.Mobile.Services;

/// <summary>
/// C# entry point to the AnchorBridge.xcframework (Swift). The framework wraps
/// Apple's FoundationModels (iOS 26+) LanguageModelSession with a @Generable output
/// schema so it returns a strict {match: Bool, confidence: Double, reason: String}
/// payload. This is the third validation gate: it reasons in natural language over
/// the Vision tags + the expected room, to detect scene mismatches (e.g. you
/// photographed the kettle but you're still at your desk).
///
/// See Bridge/README.md for the Swift source and how to build the xcframework.
/// If the xcframework is not linked yet, AnalyzeSceneAsync returns a soft-pass so
/// the app is still testable end-to-end without Apple Intelligence.
/// </summary>
public sealed class FoundationModelsClient
{
    public sealed record SceneVerdict(bool Match, float Confidence, string Reason);

    public Task<SceneVerdict> AnalyzeSceneAsync(
        IReadOnlyList<string> visionTags,
        string expectedObject,
        string expectedRoom)
    {
#if ANCHOR_FM_BRIDGE
        return AnalyzeSceneWithBridgeAsync(visionTags, expectedObject, expectedRoom);
#else
        return Task.FromResult(new SceneVerdict(true, 0.5f, "bridge-unlinked"));
#endif
    }

#if ANCHOR_FM_BRIDGE
    private static async Task<SceneVerdict> AnalyzeSceneWithBridgeAsync(
        IReadOnlyList<string> visionTags,
        string expectedObject,
        string expectedRoom)
    {
        var tagsCsv = string.Join(",", visionTags);
        var resultJson = await Task.Run(() =>
        {
            try
            {
                var ptr = anchor_fm_validate_scene(tagsCsv, expectedObject, expectedRoom);
                if (ptr == IntPtr.Zero) return null;
                var s = Marshal.PtrToStringUTF8(ptr);
                anchor_fm_free(ptr);
                return s;
            }
            catch (DllNotFoundException)
            {
                // Bridge not linked — permissive fallback for development builds.
                return "{\"match\":true,\"confidence\":0.5,\"reason\":\"bridge-unlinked\"}";
            }
            catch (EntryPointNotFoundException)
            {
                return "{\"match\":true,\"confidence\":0.5,\"reason\":\"bridge-unlinked\"}";
            }
        });

        if (string.IsNullOrWhiteSpace(resultJson))
            return new SceneVerdict(false, 0, "no-result");

        var doc = JsonDocument.Parse(resultJson).RootElement;
        return new SceneVerdict(
            doc.GetProperty("match").GetBoolean(),
            (float)doc.GetProperty("confidence").GetDouble(),
            doc.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "");
    }

    [DllImport("__Internal", EntryPoint = "anchor_fm_validate_scene", CharSet = CharSet.Ansi)]
    private static extern IntPtr anchor_fm_validate_scene(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string tagsCsv,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string expectedObject,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string expectedRoom);

    [DllImport("__Internal", EntryPoint = "anchor_fm_free")]
    private static extern void anchor_fm_free(IntPtr ptr);
#endif
}
