using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using DesktopFolders.App.Native;

namespace DesktopFolders.App.Services;

public static class ShellService
{
    public static void OpenFile(string path)
    {
        try { Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show($"无法打开: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    public static string? GetDisplayName(string path)
    {
        try
        {
            if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                var desc = new StringBuilder(260);
                if (GetShortcutDescription(path, desc)) return desc.ToString();
            }
            return Path.GetFileNameWithoutExtension(path);
        }
        catch { return Path.GetFileName(path); }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon? ExtractIcon(string path)
    {
        var info = new NativeMethods.SHFILEINFO();
        var ptr = NativeMethods.SHGetFileInfo(path, NativeMethods.FILE_ATTRIBUTE_NORMAL, ref info,
            (uint)Marshal.SizeOf<NativeMethods.SHFILEINFO>(),
            NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON | NativeMethods.SHGFI_USEFILEATTRIBUTES);
        if (ptr == IntPtr.Zero || info.hIcon == IntPtr.Zero) return null;
        var icon = Icon.FromHandle(info.hIcon);
        var clone = (Icon)icon.Clone();
        DestroyIcon(info.hIcon);
        return clone;
    }

    public static Icon? ExtractSmallIcon(string path)
    {
        var info = new NativeMethods.SHFILEINFO();
        var ptr = NativeMethods.SHGetFileInfo(path, NativeMethods.FILE_ATTRIBUTE_NORMAL, ref info,
            (uint)Marshal.SizeOf<NativeMethods.SHFILEINFO>(),
            NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_SMALLICON | NativeMethods.SHGFI_USEFILEATTRIBUTES);
        if (ptr == IntPtr.Zero || info.hIcon == IntPtr.Zero) return null;
        var icon = Icon.FromHandle(info.hIcon);
        var clone = (Icon)icon.Clone();
        DestroyIcon(info.hIcon);
        return clone;
    }

    public static string? ResolveShortcut(string shortcutPath)
    {
        var shellType = Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046"));
        if (shellType == null) return null;

        var obj = Activator.CreateInstance(shellType);
        if (obj == null) return null;

        try
        {
            var shellLink = (IShellLinkW)obj;
            shellLink.Resolve(IntPtr.Zero, 0x0001 | 0x0004);
            var path = new StringBuilder(260);
            shellLink.GetPath(path, path.Capacity, out _, 0x0001);
            return path.ToString();
        }
        catch { return null; }
    }

    private static bool GetShortcutDescription(string shortcutPath, StringBuilder desc)
    {
        try
        {
            var shellType = Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046"));
            if (shellType == null) return false;
            var obj = Activator.CreateInstance(shellType);
            if (obj == null) return false;
            var shellLink = (IShellLinkW)obj;
            shellLink.Resolve(IntPtr.Zero, 0x0001 | 0x0004);
            shellLink.GetDescription(desc, desc.Capacity);
            return desc.Length > 0;
        }
        catch { return false; }
    }

    public static bool IsShortcut(string path) => path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);
    public static bool IsExe(string path) => path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

    public static void ShowContextMenu(string filePath, Point screenPos, IntPtr ownerHwnd,
        string? extraItemText = null, Action? extraItemAction = null)
    {
        var menu = new ContextMenuStrip();
        if (extraItemText != null && extraItemAction != null)
        {
            menu.Items.Add(extraItemText, null, (s, e) => extraItemAction());
            menu.Items.Add(new ToolStripSeparator());
        }
        menu.Items.Add("打开(O)", null, (s, e) => OpenFile(filePath));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("复制(C)", null, (s, e) =>
        {
            var col = new System.Collections.Specialized.StringCollection { filePath };
            Clipboard.SetFileDropList(col);
        });
        menu.Items.Add("删除(D)", null, (s, e) =>
        {
            try { File.Delete(filePath); } catch { }
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("属性(R)", null, (s, e) =>
        {
            var args = "properties," + filePath;
            Process.Start("explorer.exe", args);
        });
        menu.Show((int)screenPos.X, (int)screenPos.Y);
    }
}

[ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellLinkW
{
    void GetPath([Out] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATA pfd, uint fFlags);
    void GetIDList(out IntPtr ppidl);
    void SetIDList(IntPtr pidl);
    void GetDescription([Out] StringBuilder pszName, int cchMaxName);
    void SetDescription(string pszName);
    void GetWorkingDirectory([Out] StringBuilder pszDir, int cchMaxPath);
    void SetWorkingDirectory(string pszDir);
    void GetArguments([Out] StringBuilder pszArgs, int cchMaxPath);
    void SetArguments(string pszArgs);
    void GetHotkey(out ushort pwHotkey);
    void SetHotkey(ushort wHotkey);
    void GetShowCmd(out int piShowCmd);
    void SetShowCmd(int iShowCmd);
    void GetIconLocation([Out] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
    void SetIconLocation(string pszIconPath, int iIcon);
    void SetRelativePath(string pszPathRel, uint dwReserved);
    void Resolve(IntPtr hwnd, uint fFlags);
    void SetPath(string pszFile);
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct WIN32_FIND_DATA
{
    public int dwFileAttributes;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
    public int nFileSizeHigh;
    public int nFileSizeLow;
    public int dwReserved0;
    public int dwReserved1;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string cFileName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
    public string cAlternateFileName;
}
