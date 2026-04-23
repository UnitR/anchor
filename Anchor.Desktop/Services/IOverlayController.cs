namespace Anchor.Desktop.Services;

/// <summary>
/// Platform-abstract contract for the non-dismissible fullscreen overlay.
/// Implementations must:
///   - Cover every connected display (not just primary).
///   - Float above every other window including fullscreen video.
///   - Swallow keyboard + mouse events so the underlying app cannot receive input,
///     with the documented exceptions (Secure Attention Sequence, Force Quit).
/// </summary>
public interface IOverlayController
{
    bool IsActive { get; }
    Task ShowAsync(ContentPage page);
    Task DismissAsync();
}

/// <summary>Fallback used when no platform implementation is available (e.g. hot reload on unsupported RID).</summary>
public sealed class NullOverlayController : IOverlayController
{
    public bool IsActive { get; private set; }
    public Task ShowAsync(ContentPage page) { IsActive = true; return Task.CompletedTask; }
    public Task DismissAsync() { IsActive = false; return Task.CompletedTask; }
}
