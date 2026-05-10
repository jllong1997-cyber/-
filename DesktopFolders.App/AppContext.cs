using DesktopFolders.App.Models;
using DesktopFolders.App.Native;
using DesktopFolders.App.Services;

namespace DesktopFolders.App;

public class AppContext : ApplicationContext
{
    internal static AppContext? Instance { get; private set; }
    internal static List<FolderData> FolderList = new();
    private readonly PersistenceService _persistence = new();
    private readonly List<FolderWindow> _windows = new();
    private NotifyIcon? _trayIcon;
    private Form? _trayForm;
    private int _hotkeyId;

    public AppContext()
    {
        Instance = this;
        LoadFolders();
        SetupTray();
        SetupHotkeys();
    }

    private void LoadFolders()
    {
        FolderList = _persistence.LoadFolders();
        if (FolderList.Count == 0)
        {
            var sw = Screen.PrimaryScreen.Bounds.Width;
            var sh = Screen.PrimaryScreen.Bounds.Height;
            FolderList.Add(new FolderData
            {
                Name = "新建文件夹",
                X = Math.Max(0, (sw - 64) / 2),
                Y = Math.Max(0, (sh - 64) / 2),
                GridCols = 2,
                GridRows = 2
            });
            _persistence.SaveFolders(FolderList);
        }

        foreach (var data in FolderList)
            CreateFolderWindow(data);
    }

    internal void CreateFolderWindow(FolderData data)
    {
        var win = new FolderWindow(data, _persistence);
        win.FormClosed += (s, e) =>
        {
            _windows.Remove(win);
            if (_windows.Count == 0) ExitThread();
        };
        _windows.Add(win);
        win.Show();
    }

    public void AddNewFolder()
    {
        var data = new FolderData
        {
            Name = "新建文件夹",
            X = 100 + _windows.Count * 20,
            Y = 100 + _windows.Count * 20,
            GridCols = 2,
            GridRows = 2
        };
        FolderList.Add(data);
        _persistence.SaveFolders(FolderList);
        CreateFolderWindow(data);
    }

    private void SetupTray()
    {
        _trayForm = new Form { ShowInTaskbar = false, WindowState = FormWindowState.Minimized };
        _trayForm.Load += (s, e) =>
        {
            _trayForm.Visible = false;
            _trayForm.ShowInTaskbar = false;
        };

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "DesktopFolders",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };

        _trayIcon.ContextMenuStrip.Items.Add("新建文件夹", null, (s, e) => AddNewFolder());
        _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        _trayIcon.ContextMenuStrip.Items.Add("全部收起", null, (s, e) =>
        {
            foreach (var w in _windows.ToList())
                w.Collapse();
        });
        _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        _trayIcon.ContextMenuStrip.Items.Add("退出", null, (s, e) =>
        {
            _trayIcon.Visible = false;
            Application.Exit();
        });
    }

    private void SetupHotkeys()
    {
        if (_trayForm == null || _trayForm.IsDisposed) return;

        var hwnd = _trayForm.Handle;

        const uint MOD_CONTROL = 0x0002;
        const uint MOD_SHIFT = 0x0004;

        if (!NativeMethods.RegisterHotKey(hwnd, ++_hotkeyId, MOD_CONTROL | MOD_SHIFT, 0x4E))
            _trayIcon?.ShowBalloonTip(3000, "DesktopFolders", "Ctrl+Shift+N 注册失败，请检查系统热键冲突", ToolTipIcon.Warning);
        if (!NativeMethods.RegisterHotKey(hwnd, ++_hotkeyId, MOD_CONTROL | MOD_SHIFT, 0x51))
            _trayIcon?.ShowBalloonTip(3000, "DesktopFolders", "Ctrl+Shift+Q 注册失败，请检查系统热键冲突", ToolTipIcon.Warning);

        WindowSubclass.Subclass(hwnd, (m, w, l) =>
        {
            const int WM_HOTKEY = 0x0312;
            if (m == WM_HOTKEY)
            {
                var id = w.ToInt32();
                if (id == _hotkeyId - 1) AddNewFolder();
                else if (id == _hotkeyId)
                {
                    _trayIcon.Visible = false;
                    Application.Exit();
                }
            }
            return IntPtr.Zero;
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon?.Dispose();
            _trayForm?.Dispose();
            for (int i = 0; i < _hotkeyId; i++)
                NativeMethods.UnregisterHotKey(_trayForm?.Handle ?? IntPtr.Zero, i + 1);
        }
        base.Dispose(disposing);
    }
}
