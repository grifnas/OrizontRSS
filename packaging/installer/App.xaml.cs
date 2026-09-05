using System.Windows;

namespace OrizontSetup;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindow = new MainWindow(e.Args.Any(a => string.Equals(a, "/uninstall", StringComparison.OrdinalIgnoreCase)));
        MainWindow.Show();
    }
}
