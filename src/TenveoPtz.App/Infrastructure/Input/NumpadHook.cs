using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TenveoPtz.App.Infrastructure.Input;

/// <summary>
/// System-wide hook on the numeric keypad (WH_KEYBOARD_LL), so its keys can drive the camera while
/// another application has the focus. Only keypad keys are reported: the dedicated arrow keys and
/// the digit row are told apart by the "extended" flag and left alone. Key presses aimed at this
/// application's own windows are not reported either; its normal keyboard handling sees them.
/// The callback runs on the thread that created the hook (the UI thread) and must return quickly.
/// </summary>
internal sealed class NumpadHook : IDisposable
{
    private const int WhKeyboardLowLevel = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int FlagExtended = 0x01;

    private readonly Func<Keys, bool> keyDown;
    private readonly Func<Keys, bool> keyUp;
    private readonly HookProc callback;
    private readonly int processId = Process.GetCurrentProcess().Id;
    private IntPtr hook;

    /// <param name="keyDown">Called for a keypad key pressed while another application is in front; returns whether to swallow it.</param>
    /// <param name="keyUp">Called for every keypad key release; returns whether to swallow it.</param>
    public NumpadHook(Func<Keys, bool> keyDown, Func<Keys, bool> keyUp)
    {
        this.keyDown = keyDown ?? throw new ArgumentNullException(nameof(keyDown));
        this.keyUp = keyUp ?? throw new ArgumentNullException(nameof(keyUp));
        callback = OnKey; // Kept in a field: the delegate must outlive the native hook.
        hook = SetWindowsHookEx(WhKeyboardLowLevel, callback, GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot watch the numeric keypad");
        }
    }

    public void Dispose()
    {
        if (hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(hook);
            hook = IntPtr.Zero;
        }
    }

    /// <summary>The key a keypad key stands for, or <see cref="Keys.None"/> for any other key.</summary>
    internal static Keys KeypadKey(int virtualKey, int flags)
    {
        var key = (Keys)virtualKey;
        if (key is >= Keys.NumPad1 and <= Keys.NumPad9 or Keys.Add or Keys.Subtract)
        {
            return key;
        }

        // With Num Lock off the keypad sends arrows, Home and Page Up/Down without the extended
        // flag; the dedicated keys of the same name have it.
        var navigation = key is Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.Home or Keys.PageUp or Keys.PageDown;
        return navigation && (flags & FlagExtended) == 0 ? key : Keys.None;
    }

    private IntPtr OnKey(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            var info = Marshal.PtrToStructure<KeyboardInfo>(data);
            // Injected keys count too: macro pads and remapping tools (Stream Deck, AutoHotkey) send them.
            var key = KeypadKey(info.VirtualKey, info.Flags);
            if (key != Keys.None)
            {
                var swallow = (int)message switch
                {
                    WmKeyDown or WmSysKeyDown => !IsOwnWindowInFront() && keyDown(key),
                    WmKeyUp or WmSysKeyUp => keyUp(key),
                    _ => false,
                };
                if (swallow)
                {
                    return (IntPtr)1;
                }
            }
        }

        return CallNextHookEx(hook, code, message, data);
    }

    private bool IsOwnWindowInFront()
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out var foregroundProcess);
        return foregroundProcess == processId;
    }

    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInfo
    {
        public int VirtualKey;
        public int ScanCode;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookType, HookProc callback, IntPtr module, int threadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr window, out int processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
