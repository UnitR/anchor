#if MACCATALYST
using System.Runtime.InteropServices;
using CoreFoundation;

namespace Anchor.Desktop.Platforms.MacCatalyst;

/// <summary>
/// CGEventTap-based swallowing of key/mouse events while the overlay is up.
/// Runs on its own CFRunLoop thread so that even if the main thread is blocked
/// the tap keeps filtering.
/// Known exceptions (OS-enforced): Cmd-Opt-Esc (Force Quit), Secure Input password prompts.
/// </summary>
public sealed class MacInputBlocker
{
    private IntPtr _tap;
    private CFRunLoop? _loop;
    private Thread? _thread;

    public void Start()
    {
        _thread = new Thread(() =>
        {
            var mask = CGEventMaskBit(CGEventType.KeyDown)
                     | CGEventMaskBit(CGEventType.KeyUp)
                     | CGEventMaskBit(CGEventType.FlagsChanged)
                     | CGEventMaskBit(CGEventType.LeftMouseDown)
                     | CGEventMaskBit(CGEventType.RightMouseDown)
                     | CGEventMaskBit(CGEventType.OtherMouseDown)
                     | CGEventMaskBit(CGEventType.ScrollWheel);

            _tap = CGEventTapCreate(
                tap: 0,     // kCGSessionEventTap
                place: 0,   // kCGHeadInsertEventTap
                options: 0, // kCGEventTapOptionDefault
                eventsOfInterest: mask,
                callback: SwallowCallback,
                userInfo: IntPtr.Zero);
            if (_tap == IntPtr.Zero) return;

            var source = CFMachPortCreateRunLoopSource(IntPtr.Zero, _tap, 0);
            _loop = CFRunLoop.Current;
            CFRunLoopAddSource(_loop.Handle, source, CFRunLoop.ModeCommon.Handle);
            CGEventTapEnable(_tap, true);
            CFRunLoop.Current.Run();
        }) { IsBackground = true };
        _thread.Start();
    }

    public void Stop()
    {
        if (_tap != IntPtr.Zero) CGEventTapEnable(_tap, false);
        _loop?.Stop();
    }

    private static IntPtr SwallowCallback(IntPtr proxy, CGEventType type, IntPtr evt, IntPtr userInfo)
        => IntPtr.Zero; // returning null discards the event

    private enum CGEventType : uint
    {
        KeyDown = 10, KeyUp = 11, FlagsChanged = 12,
        LeftMouseDown = 1, RightMouseDown = 3, OtherMouseDown = 25,
        ScrollWheel = 22,
    }

    private static ulong CGEventMaskBit(CGEventType t) => 1UL << (int)t;

    private delegate IntPtr CGEventTapCallBack(IntPtr proxy, CGEventType type, IntPtr evt, IntPtr userInfo);

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern IntPtr CGEventTapCreate(uint tap, uint place, uint options, ulong eventsOfInterest, CGEventTapCallBack callback, IntPtr userInfo);

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern void CGEventTapEnable(IntPtr tap, [MarshalAs(UnmanagedType.I1)] bool enable);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr port, nint order);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopAddSource(IntPtr loop, IntPtr source, IntPtr mode);
}
#endif
