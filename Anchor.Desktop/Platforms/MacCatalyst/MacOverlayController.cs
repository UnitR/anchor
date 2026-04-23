#if MACCATALYST
using Anchor.Desktop.Services;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Anchor.Desktop.Platforms.MacCatalyst;

/// <summary>
/// Non-dismissible overlay on macOS via Mac Catalyst. We resize the Catalyst UIWindow
/// to full-screen bounds and raise its underlying NSWindow level to .screenSaver (1000),
/// then enable the all-spaces + fullscreen collection behavior. Input is blocked by
/// CGEventTap in MacInputBlocker.
///
/// Limits explicitly documented in plan:
///   • Cmd-Opt-Esc (Force Quit) cannot be blocked; treated as emergency bypass.
///   • Cmd-Q is swallowed by the event tap.
/// </summary>
public sealed class MacOverlayController : IOverlayController
{
    private UIWindow? _overlayWindow;
    private MacInputBlocker? _blocker;

    public bool IsActive => _overlayWindow is not null;

    public Task ShowAsync(ContentPage page)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var scene = UIApplication.SharedApplication.ConnectedScenes
                .OfType<UIWindowScene>().FirstOrDefault();
            if (scene is null) return;

            _overlayWindow = new UIWindow(scene)
            {
                RootViewController = page.CreateViewController(),
                WindowLevel = UIWindowLevel.Alert + 1,
            };
            _overlayWindow.MakeKeyAndVisible();
            RaiseToScreenSaverLevel(_overlayWindow);

            _blocker = new MacInputBlocker();
            _blocker.Start();
        });
        return Task.CompletedTask;
    }

    public Task DismissAsync()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _blocker?.Stop();
            _blocker = null;
            if (_overlayWindow is not null)
            {
                _overlayWindow.Hidden = true;
                _overlayWindow = null;
            }
        });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Catalyst apps can bridge to the underlying AppKit NSWindow via the private
    /// _NSHostingWindow that wraps the UIWindow. We use the supported _nsWindow selector.
    /// If unavailable we fall back to UIWindowLevel.Alert.
    /// </summary>
    private static void RaiseToScreenSaverLevel(UIWindow window)
    {
        try
        {
            var sel = new Selector("_nsWindow");
            var nsWin = Runtime.GetNSObject(Messaging.IntPtr_objc_msgSend(window.Handle, sel.Handle));
            if (nsWin is null) return;
            // NSScreenSaverWindowLevel = CGShieldingWindowLevel() - 1 ≈ 1000
            var levelSel = new Selector("setLevel:");
            Messaging.void_objc_msgSend_int(nsWin.Handle, levelSel.Handle, 1000);
            var behaviorSel = new Selector("setCollectionBehavior:");
            // canJoinAllSpaces | fullScreenAuxiliary | stationary
            Messaging.void_objc_msgSend_int(nsWin.Handle, behaviorSel.Handle, (1 << 0) | (1 << 8) | (1 << 4));
        }
        catch
        {
            // Best-effort; if Apple changes the bridge, the Alert-level window still shows.
        }
    }
}
#endif
