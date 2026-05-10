# DesktopFolders

Windows 桌面应用文件夹工具。每个文件夹以**小图标**形式悬浮在桌面上，点击展开为图标网格。

## 系统要求

- Windows 10 / Windows 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## 用法

### 启动

双击 `DesktopFolders.App.exe`。桌面出现一个 64×64 的深色圆角小方块。

### 交互

| 操作 | 效果 |
|------|------|
| **左键单击** 小方块 | 展开文件夹（2×2 或 3×3 图标网格） |
| **左键单击** 外部 | 自动收起文件夹 |
| **拖拽标题栏** | 移动文件夹窗口 |
| **双击标题** | 重命名文件夹 |
| **右键小方块** | 菜单：重命名 / 更改颜色 / 删除 |
| **拖文件** 到展开的文件夹 | 添加到图标列表 |
| **双击图标** | 启动程序 |
| **右键图标** | 快捷菜单（打开/复制/删除/属性） |

### 新建文件夹

| 方式 | 操作 |
|------|------|
| 快捷键 | `Ctrl + Shift + N` |
| 托盘 | 右键托盘图标 → "新建文件夹" |

### 退出

| 方式 | 操作 |
|------|------|
| 快捷键 | `Ctrl + Shift + Q` |
| 托盘 | 右键托盘图标 → "退出" |

### 自定义

- **颜色**：右键小方块 → "更改颜色" → 选择颜色
- **名称**：右键小方块 → "重命名" 或展开后双击标题栏

## 存储

布局自动保存到：
```
%LocalAppData%/DesktopFolders/folders.json
```

## 技术说明

- **框架**：WinForms .NET 8
- **窗口**：无边框，有状态切换（关闭=64×64，展开=2×2/3×3 网格）
- **渲染**：标准 WinForms 控件（Label + PictureBox + FlowLayoutPanel）
- **热键**：Win32 RegisterHotKey (Ctrl+Shift+N / Ctrl+Shift+Q)
- **持久化**：JSON

## 构建

```bash
dotnet restore
dotnet build -c Release
```

## 项目结构

```
src/DesktopFolders.App/
├── Program.cs                  # 入口
├── AppContext.cs               # 上下文 (托盘、热键、文件夹管理)
├── FolderWindow.cs             # 文件夹窗口 (关闭/展开两态)
├── WindowSubclass.cs           # Win32 子类化
├── Models/FolderData.cs        # 数据模型
├── Services/
│   ├── PersistenceService.cs   # JSON 持久化
│   ├── ShellService.cs         # Shell API
│   └── DesktopIntegration.cs   # 窗口样式
└── Native/NativeMethods.cs     # P/Invoke
```
