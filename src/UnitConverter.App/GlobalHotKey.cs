using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace UnitConverter.App;

/// <summary>A system-wide keyboard shortcut bound to a window's message loop.</summary>
public sealed class GlobalHotKey : IDisposable
{
    private const int WmHotKey = 0x0312;
    private const int Id = 0x5543; // "UC"

    [Flags]
    public enum Modifiers : uint { Alt = 0x1, Control = 0x2, Shift = 0x4, Win = 0x8, NoRepeat = 0x4000 }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HwndSource _source;
    private readonly Action _onPressed;

    private GlobalHotKey(HwndSource source, Action onPressed)
    {
        _source = source;
        _onPressed = onPressed;
        _source.AddHook(WndProc);
    }

    /// <summary>Returns null when another app already owns the shortcut.</summary>
    public static GlobalHotKey? TryRegister(Window window, Modifiers modifiers, System.Windows.Input.Key key, Action onPressed)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();
        var vk = (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
        if (!RegisterHotKey(handle, Id, (uint)(modifiers | Modifiers.NoRepeat), vk))
            return null;
        return new GlobalHotKey(HwndSource.FromHwnd(handle), onPressed);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == Id)
        {
            _onPressed();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _source.RemoveHook(WndProc);
        UnregisterHotKey(_source.Handle, Id);
    }
}
