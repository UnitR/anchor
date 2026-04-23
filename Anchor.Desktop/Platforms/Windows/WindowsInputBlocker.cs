#if WINDOWS
using System.Runtime.InteropServices;

namespace Anchor.Desktop.Platforms.Windows;

/// <summary>
/// Low-level keyboard + mouse hook that swallows events while the overlay is up.
/// Cannot block SAS (Ctrl-Alt-Del) or UAC — those are handled by logging as emergency bypass.
/// </summary>
public sealed class WindowsInputBlocker
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private IntPtr _kbHook;
    private IntPtr _mouseHook;
    private LowLevelProc? _kbProc;
    private LowLevelProc? _mouseProc;

    public void Start()
    {
        _kbProc = (nCode, wParam, lParam) => nCode >= 0 ? (IntPtr)1 : CallNextHookEx(_kbHook, nCode, wParam, lParam);
        _mouseProc = (nCode, wParam, lParam) => nCode >= 0 ? (IntPtr)1 : CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        var mod = GetModuleHandle(null);
        _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, mod, 0);
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, mod, 0);
    }

    public void Stop()
    {
        if (_kbHook != IntPtr.Zero) UnhookWindowsHookEx(_kbHook);
        if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
        _kbHook = _mouseHook = IntPtr.Zero;
    }

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
#endif
