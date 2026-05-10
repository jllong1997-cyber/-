using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic;
using DesktopFolders.App.Models;
using DesktopFolders.App.Native;
using DesktopFolders.App.Services;

namespace DesktopFolders.App;

public class FolderWindow : Form
{
    private readonly FolderData _data;
    private readonly PersistenceService _persistence;
    private readonly FlowLayoutPanel _iconPanel;
    private readonly Panel _titleBar;
    private readonly Label _titleLabel;
    private readonly Panel _closedPanel;
    private readonly Button _addBtn;
    private readonly Button _closeBtn;
    private readonly Button _delBtn;

    private bool _dragActive;
    private Point _dragMouseStart;
    private Point _dragFormStart;
    private bool _inModalDialog;

    private const int ClosedSize = 64;
    private static readonly Size Expanded2x2Base = new(196, 226);
    private static readonly Size Expanded3x3Base = new(276, 306);

    private Size Expanded2x2 => ScaleSize(Expanded2x2Base);
    private Size Expanded3x3 => ScaleSize(Expanded3x3Base);
    private int ScaledClosedSize => Scale(ClosedSize);

    public FolderData Data => _data;

    public FolderWindow(FolderData data, PersistenceService persistence)
    {
        _data = data;
        _persistence = persistence;

        Text = data.Name;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(data.X, data.Y);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        BackColor = data.GetColor();

        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);

        var hwnd = Handle;
        DesktopIntegration.StyleWindow(hwnd);
        DesktopIntegration.SetDarkTitleBar(hwnd);
        NativeMethods.DragAcceptFiles(hwnd, true);

        // Bypass UIPI so WM_DROPFILES from Explorer (higher integrity level) reaches us
        var filterStruct = new NativeMethods.CHANGEFILTERSTRUCT
            { cbSize = (uint)Marshal.SizeOf<NativeMethods.CHANGEFILTERSTRUCT>() };
        NativeMethods.ChangeWindowMessageFilterEx(hwnd, 0x0233, NativeMethods.MSGFLT_ADD, ref filterStruct);

        // OLE drag-drop as additional mechanism
        AllowDrop = true;
        DragEnter += (_, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
        DragDrop += (_, e) =>
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                bool changed = false;
                foreach (var f in files)
                {
                    if (!_data.IconPaths.Contains(f))
                    {
                        _data.IconPaths.Add(f);
                        changed = true;
                    }
                }
                if (changed)
                {
                    _persistence.SaveFolders(AppContext.FolderList);
                    if (_data.IsExpanded) ReloadIcons();
                }
            }
        };

        _closedPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = data.GetColor(),
            Cursor = Cursors.Hand
        };
        _closedPanel.Paint += (s, e) =>
        {
            var r = _closedPanel.ClientRectangle;
            using var path = MakeRoundRect(r, Scale(10));
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var bg = new SolidBrush(_data.GetColor());
            e.Graphics.FillPath(bg, path);
            using var border = new Pen(Color.FromArgb(120, 180, 200, 255), 2);
            e.Graphics.DrawPath(border, path);
            using var font = new Font("Segoe UI", 9, FontStyle.Bold);
            e.Graphics.DrawString(_data.Name, font, Brushes.White, Scale(6), Scale(4));
            int cnt = _data.IconPaths.Count;
            if (cnt > 0)
            {
                using var f2 = new Font("Segoe UI", 7);
                e.Graphics.DrawString(cnt + "项", f2, Brushes.LightGray, Scale(6), r.Height - Scale(18));
            }
        };

        _closedPanel.MouseDown += (s, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _dragActive = true;
            _dragMouseStart = e.Location;
            _dragFormStart = Location;
            _closedPanel.Capture = true;
        };
        _closedPanel.MouseMove += (s, e) =>
        {
            if (!_dragActive) return;
            var dx = e.X - _dragMouseStart.X;
            var dy = e.Y - _dragMouseStart.Y;
            if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
                Location = new Point(_dragFormStart.X + dx, _dragFormStart.Y + dy);
        };
        _closedPanel.MouseUp += (s, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _dragActive = false;
            _closedPanel.Capture = false;
            if (Math.Abs(e.X - _dragMouseStart.X) < 5 && Math.Abs(e.Y - _dragMouseStart.Y) < 5)
                Expand();
            _data.X = Location.X;
            _data.Y = Location.Y;
            _persistence.SaveFolders(AppContext.FolderList);
        };
        _closedPanel.MouseClick += (s, e) => { if (e.Button == MouseButtons.Right) ShowClosedMenu(e.Location); };

        _titleBar = new Panel
        {
            Height = Scale(32),
            Dock = DockStyle.Top,
            BackColor = Darken(data.GetColor(), 10),
            Cursor = Cursors.SizeAll,
            Visible = false
        };
        _titleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _dragActive = true; _dragMouseStart = e.Location; _dragFormStart = Location; _titleBar.Capture = true; } };
        _titleBar.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left && _dragActive) Location = new Point(_dragFormStart.X + e.X - _dragMouseStart.X, _dragFormStart.Y + e.Y - _dragMouseStart.Y); };
        _titleBar.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) { _dragActive = false; _titleBar.Capture = false; _data.X = Location.X; _data.Y = Location.Y; _persistence.SaveFolders(AppContext.FolderList); } };

        _titleLabel = new Label
        {
            Text = data.Name, ForeColor = Color.White, BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(Scale(10), 0), Size = new Size(Scale(120), Scale(32)),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _titleLabel.DoubleClick += (s, e) =>
        {
            _inModalDialog = true;
            var input = Interaction.InputBox("重命名:", "重命名文件夹", _data.Name);
            _inModalDialog = false;
            if (IsValidFolderName(input))
            {
                _data.Name = input; _titleLabel.Text = input; Text = input;
                _closedPanel.Invalidate();
                _persistence.SaveFolders(AppContext.FolderList);
            }
        };

        int btnSize = Scale(24);
        int btnGap = Scale(28);

        _addBtn = TitleBarButton("+", btnSize);
        _addBtn.Location = new Point(GridRight() - btnGap * 2, Scale(4));
        _addBtn.Click += (s, e) => AppContext.Instance?.AddNewFolder();

        _closeBtn = TitleBarButton("✕", btnSize);
        _closeBtn.Location = new Point(GridRight(), Scale(4));
        _closeBtn.Click += (s, e) => Collapse();

        _delBtn = TitleBarButton("🗑", btnSize);
        _delBtn.Location = new Point(GridRight() - btnGap * 3, Scale(4));
        _delBtn.Click += (s, e) =>
        {
            if (MessageBox.Show("删除此文件夹？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { AppContext.FolderList.Remove(_data); _persistence.SaveFolders(AppContext.FolderList); Close(); }
        };

        _titleBar.Controls.Add(_titleLabel);
        _titleBar.Controls.Add(_addBtn);
        _titleBar.Controls.Add(_closeBtn);
        _titleBar.Controls.Add(_delBtn);

        _iconPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Color.FromArgb(45, 45, 60),
            Padding = new Padding(Scale(8)), AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Visible = false
        };

        MinimumSize = new Size(ScaledClosedSize, ScaledClosedSize);

        Controls.Add(_iconPanel);
        Controls.Add(_titleBar);
        Controls.Add(_closedPanel);

        this.Shown += (s, e) =>
        {
            DesktopIntegration.PlaceBelowDesktop(Handle);
            if (_data.IsExpanded) Expand(); else Collapse();
        };
    }

    private static Button TitleBarButton(string text, int size)
    {
        return new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            ForeColor = Color.LightGray,
            BackColor = Color.Transparent,
            Size = new Size(size, size),
            Cursor = Cursors.Hand
        };
    }

    private void ShowClosedMenu(Point loc)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("重命名", null, (s2, e2) =>
        {
            _inModalDialog = true;
            var input = Interaction.InputBox("重命名:", "重命名文件夹", _data.Name);
            _inModalDialog = false;
            if (IsValidFolderName(input))
            {
                _data.Name = input; _titleLabel.Text = input; Text = input;
                _closedPanel.Invalidate();
                _persistence.SaveFolders(AppContext.FolderList);
            }
        });
        menu.Items.Add("更改颜色", null, (s2, e2) =>
        {
            _inModalDialog = true;
            var cd = new ColorDialog { Color = _data.GetColor() };
            if (cd.ShowDialog() == DialogResult.OK)
            {
                _data.SetColor(cd.Color);
                _closedPanel.BackColor = cd.Color;
                _titleBar.BackColor = Darken(cd.Color, 10);
                _closedPanel.Invalidate();
                _persistence.SaveFolders(AppContext.FolderList);
            }
            _inModalDialog = false;
        });
        menu.Items.Add(new ToolStripSeparator());
        var gridItem = new ToolStripMenuItem("网格大小");
        var twoByTwo = gridItem.DropDownItems.Add("2 × 2", null, (s2, e2) => SetGridSize(2, 2));
        var threeByThree = gridItem.DropDownItems.Add("3 × 3", null, (s2, e2) => SetGridSize(3, 3));
        if (_data.GridCols == 2) twoByTwo.Select();
        else threeByThree.Select();
        menu.Items.Add(gridItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("删除", null, (s2, e2) =>
        {
            if (MessageBox.Show("删除此文件夹？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { AppContext.FolderList.Remove(_data); _persistence.SaveFolders(AppContext.FolderList); Close(); }
        });
        menu.Show(_closedPanel, loc);
    }

    public void Expand()
    {
        _data.IsExpanded = true;
        _closedPanel.Visible = false;
        _titleBar.Visible = true;
        _iconPanel.Visible = true;
        ReloadIcons();
        Location = new Point(_data.X, _data.Y);
        var target = GetExpandedSize();
        Animate(new Size(ScaledClosedSize, ScaledClosedSize), target, 8, 16, null);
    }

    public void Collapse()
    {
        _data.IsExpanded = false;
        _data.X = Location.X;
        _data.Y = Location.Y;
        var start = Size;
        Animate(start, new Size(ScaledClosedSize, ScaledClosedSize), 6, 16, () =>
        {
            _titleBar.Visible = false;
            _iconPanel.Visible = false;
            _closedPanel.Visible = true;
            _closedPanel.Invalidate();
            DesktopIntegration.PlaceBelowDesktop(Handle);
            _persistence.SaveFolders(AppContext.FolderList);
        });
    }

    private void Animate(Size from, Size to, int steps, int intervalMs, Action? onDone)
    {
        int step = 0;
        var timer = new System.Windows.Forms.Timer { Interval = intervalMs };
        timer.Tick += (_, _) =>
        {
            step++;
            float t = Math.Min(1f, step / (float)steps);
            t = t * (2 - t);
            Width = (int)(from.Width + (to.Width - from.Width) * t);
            Height = (int)(from.Height + (to.Height - from.Height) * t);
            if (step >= steps)
            {
                timer.Stop();
                timer.Dispose();
                onDone?.Invoke();
            }
        };
        timer.Start();
    }

    private void ReloadIcons()
    {
        _iconPanel.Controls.Clear();
        int itemW = Scale(80), itemH = Scale(88);
        int picW = Scale(36), picH = Scale(36);
        int lblW = Scale(76), lblH = Scale(32);
        foreach (var path in _data.IconPaths)
        {
            var icon = ShellService.ExtractSmallIcon(path);
            var name = ShellService.GetDisplayName(path) ?? Path.GetFileName(path);

            var item = new Panel { Width = itemW, Height = itemH, BackColor = Color.FromArgb(40, 255, 255, 255), Margin = new Padding(Scale(4)), Cursor = Cursors.Hand, Tag = path };
            var pic = new PictureBox { Width = picW, Height = picH, Image = icon?.ToBitmap(), SizeMode = PictureBoxSizeMode.Zoom, Location = new Point(Scale(22), Scale(8)), BackColor = Color.Transparent };
            var lbl = new Label { Text = name, ForeColor = Color.FromArgb(200, 255, 255, 255), BackColor = Color.Transparent, Font = new Font("Segoe UI", 8), TextAlign = ContentAlignment.MiddleCenter, Width = lblW, Height = lblH, Location = new Point(Scale(2), Scale(50)), AutoEllipsis = true };

            item.Controls.Add(pic);
            item.Controls.Add(lbl);

            item.MouseDown += (s, me) =>
            {
                if (me.Button == MouseButtons.Left && me.Clicks == 2) { var p = (string)((Control)s!).Tag!; ShellService.OpenFile(p); }
                if (me.Button == MouseButtons.Right)
                {
                    var p = (string)((Control)s!).Tag!;
                    ShellService.ShowContextMenu(p, item.PointToScreen(me.Location), Handle,
                        "从文件夹移除", () => RemoveIcon(p));
                }
            };

            _iconPanel.Controls.Add(item);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_closedPanel != null) _closedPanel.Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_DROPFILES = 0x0233;
        if (m.Msg == WM_DROPFILES)
        {
            OnNativeFileDrop(m.WParam);
            return;
        }
        base.WndProc(ref m);
    }

    private void OnNativeFileDrop(IntPtr hDrop)
    {
        var sb = new StringBuilder(260);
        uint count = NativeMethods.DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
        bool changed = false;
        for (uint i = 0; i < count; i++)
        {
            NativeMethods.DragQueryFile(hDrop, i, sb, sb.Capacity);
            var f = sb.ToString();
            if (!_data.IconPaths.Contains(f))
            {
                _data.IconPaths.Add(f);
                changed = true;
            }
        }
        NativeMethods.DragFinish(hDrop);
        if (changed)
        {
            _persistence.SaveFolders(AppContext.FolderList);
            if (_data.IsExpanded) ReloadIcons();
        }
    }

    private void RemoveIcon(string path)
    {
        _data.IconPaths.Remove(path);
        _persistence.SaveFolders(AppContext.FolderList);
        if (_data.IsExpanded) ReloadIcons();
    }

    private static bool IsValidFolderName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name)
            && name.Length <= 64
            && name.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;
    }

    private int GridRight() => _data.GridCols == 2 ? Scale(170) : Scale(250);

    private void SetGridSize(int cols, int rows)
    {
        _data.GridCols = cols;
        _data.GridRows = rows;
        int btnGap = Scale(28);
        _addBtn.Location = new Point(GridRight() - btnGap * 2, Scale(4));
        _closeBtn.Location = new Point(GridRight(), Scale(4));
        _delBtn.Location = new Point(GridRight() - btnGap * 3, Scale(4));
        if (_data.IsExpanded)
            Size = GetExpandedSize();
        _persistence.SaveFolders(AppContext.FolderList);
    }

    private Size GetExpandedSize() =>
        _data.GridCols == 2 && _data.GridRows == 2 ? Expanded2x2 : Expanded3x3;

    private int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96.0);
    private Size ScaleSize(Size s) => new(Scale(s.Width), Scale(s.Height));

    private static System.Drawing.Drawing2D.GraphicsPath MakeRoundRect(Rectangle r, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color Darken(Color c, int amount)
    {
        return Color.FromArgb(
            Math.Max(0, c.R - amount),
            Math.Max(0, c.G - amount),
            Math.Max(0, c.B - amount));
    }
}
