#if WINDOWS
using Anchor.Desktop.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace Anchor.Desktop.Platforms.Windows;

/// <summary>
/// Non-dismissible overlay on Windows via a borderless, topmost WinUI Window sized to
/// the virtual screen rect (covers all monitors). Input is blocked globally by
/// WindowsInputBlocker using a WH_KEYBOARD_LL + WH_MOUSE_LL hook.
///
/// Documented limitation: Ctrl-Alt-Del (SAS) and UAC elevation prompts cannot be
/// blocked without a kernel driver; we treat these as implicit emergency bypass and
/// log the occurrence.
/// </summary>
public sealed class WindowsOverlayController : IOverlayController
{
    private Window? _window;
    private WindowsInputBlocker? _blocker;

    public bool IsActive => _window is not null;

    public Task ShowAsync(ContentPage page)
    {
        var handler = page.Handler;
        _window = new Window
        {
            Content = page.ToPlatform(Microsoft.Maui.Controls.Application.Current!.Handler!.MauiContext!),
            ExtendsContentIntoTitleBar = true,
        };

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);

        // Make borderless fullscreen across all monitors (virtual screen rect).
        var virt = WindowsDisplayInfo.VirtualScreenRect();
        appWindow.MoveAndResize(virt);
        appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

        _window.Activate();

        // Always-topmost behaviour: set GWL_EXSTYLE + HWND_TOPMOST.
        WindowsTopmost.MakeTopmost(hwnd);

        _blocker = new WindowsInputBlocker();
        _blocker.Start();

        return Task.CompletedTask;
    }

    public Task DismissAsync()
    {
        _blocker?.Stop();
        _blocker = null;
        _window?.Close();
        _window = null;
        return Task.CompletedTask;
    }
}
#endif
