using System.Text;
using DesktopFolders.App.Native;

namespace DesktopFolders.App.Services;

public static class DesktopIntegration
{
    public static void StyleWindow(IntPtr hwnd)
    {
        var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
        exStyle |= NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle);
    }

    public static void SetDarkTitleBar(IntPtr hwnd)
    {
        if (Environment.OSVersion.Version.Major < 10) return;
        var attr = 1;
        NativeMethods.DwmSetWindowAttribute(hwnd, 20, ref attr, sizeof(int));
        var useImmersive = 1;
        NativeMethods.DwmSetWindowAttribute(hwnd, 19, ref useImmersive, sizeof(int));
    }

    /// <summary>
    /// Position the window behind all normal apps, above the desktop icons.
    /// Uses z-order (not SetParent) so drag-drop still works.
    /// </summary>
    public static void PlaceBelowDesktop(IntPtr hwnd)
    {
        // Find the WorkerW that hosts SHELLDLL_DefView (desktop icons)
        IntPtr workerw = IntPtr.Zero;
        var sb = new StringBuilder(256);
        NativeMethods.EnumWindows((wnd, param) =>
        {
            NativeMethods.GetClassName(wnd, sb, sb.Capacity);
            if (sb.ToString() == "WorkerW")
            {
                var child = NativeMethods.FindWindowEx(wnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (child != IntPtr.Zero)
                {
                    workerw = wnd;
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);

        if (workerw != IntPtr.Zero)
            NativeMethods.SetWindowPos(hwnd, workerw, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }
}
