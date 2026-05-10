using System.Runtime.InteropServices;
using DesktopFolders.App.Native;

namespace DesktopFolders.App;

internal static class WindowSubclass
{
    private static readonly Dictionary<IntPtr, SubclassInfo> _subclasses = new();

    public static void Subclass(IntPtr hwnd, Func<uint, IntPtr, IntPtr, IntPtr> filter)
    {
        var proc = new NativeMethods.WindowProc(WndProc);
        var info = new SubclassInfo
        {
            Hwnd = hwnd,
            Filter = filter,
            Delegate = proc,
            OriginalProc = GetWindowLongPtr(hwnd, -4)
        };

        _subclasses[hwnd] = info;
        SetWindowLongPtr(hwnd, -4, Marshal.GetFunctionPointerForDelegate(proc));
    }

    private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (_subclasses.TryGetValue(hwnd, out var info))
        {
            var result = info.Filter(msg, wParam, lParam);
            if (result != IntPtr.Zero) return result;
            return CallWindowProc(info.OriginalProc, hwnd, msg, wParam, lParam);
        }
        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private class SubclassInfo
    {
        public IntPtr Hwnd;
        public Func<uint, IntPtr, IntPtr, IntPtr> Filter = null!;
        public IntPtr OriginalProc;
        public NativeMethods.WindowProc? Delegate;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
}
