namespace DesktopFolders.App.Models;

public class FolderData
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "新建文件夹";
    public int X { get; set; } = 200;
    public int Y { get; set; } = 200;
    public int GridCols { get; set; } = 2;
    public int GridRows { get; set; } = 2;
    public int ColorArgb { get; set; } = -1;
    public bool IsExpanded { get; set; }
    public List<string> IconPaths { get; set; } = new();

    public Color GetColor()
    {
        if (ColorArgb == -1) return Color.FromArgb(50, 60, 90);
        return Color.FromArgb(ColorArgb);
    }

    public void SetColor(Color c)
    {
        ColorArgb = c.ToArgb();
    }
}
