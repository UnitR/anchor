#if MACCATALYST
using Anchor.Desktop.Platforms.MacCatalyst;
using Anchor.Desktop.Services;
using Foundation;
using ObjCRuntime;
using Microsoft.Maui.Platform;
using UIKit;

namespace Anchor.Desktop;

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
                RootViewController = CreateOverlayViewController(page),
                WindowLevel = UIWindowLevel.Alert + 1,
            };
            _overlayWindow.MakeKeyAndVisible();
            RaiseToScreenSaverLevel(_overlayWindow);

            _blocker = new MacInputBlocker();
            _blocker.Start();
        });
        return Task.CompletedTask;
    }

    private static UIViewController CreateOverlayViewController(ContentPage page)
    {
        var mauiContext = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.MauiContext
            ?? Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext
            ?? throw new InvalidOperationException("A MAUI context is required to present the overlay.");

        var nativeView = page.ToPlatform(mauiContext);
        nativeView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

        return new UIViewController
        {
            View = nativeView,
        };
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
            var key = new NSString("_nsWindow");
            if (!window.RespondsToSelector(new Selector("_nsWindow")))
                return;

            var nsWin = window.ValueForKey(key);
            if (nsWin is null) return;

            // NSScreenSaverWindowLevel = CGShieldingWindowLevel() - 1 ≈ 1000
            nsWin.SetValueForKey(NSNumber.FromInt32(1000), new NSString("level"));
            // canJoinAllSpaces | fullScreenAuxiliary | stationary
            nsWin.SetValueForKey(NSNumber.FromInt32((1 << 0) | (1 << 8) | (1 << 4)), new NSString("collectionBehavior"));
        }
        catch
        {
            // Best-effort; if Apple changes the bridge, the Alert-level window still shows.
        }
    }
}
#endif
