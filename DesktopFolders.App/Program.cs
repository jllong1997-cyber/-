namespace DesktopFolders.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        bool createdNew;
        using var mutex = new Mutex(true, "DesktopFolders.App", out createdNew);
        if (!createdNew) return;

        ApplicationConfiguration.Initialize();

        try
        {
            Application.Run(new AppContext());
        }
        catch (Exception ex)
        {
            MessageBox.Show("Startup error:\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
