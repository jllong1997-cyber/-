using System.Text.Json;

namespace DesktopFolders.App.Services;

public class PersistenceService
{
    private readonly string _dir;

    public PersistenceService()
    {
        _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopFolders");
        Directory.CreateDirectory(_dir);
    }

    public void SaveFolders(List<Models.FolderData> folders)
    {
        var path = Path.Combine(_dir, "folders.json");
        var json = JsonSerializer.Serialize(folders, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public List<Models.FolderData> LoadFolders()
    {
        var path = Path.Combine(_dir, "folders.json");
        if (!File.Exists(path)) return new List<Models.FolderData>();
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<Models.FolderData>>(json) ?? new List<Models.FolderData>();
        }
        catch
        {
            var backup = path + ".bak";
            try { File.Copy(path, backup, overwrite: true); } catch { }
            return new List<Models.FolderData>();
        }
    }

}
