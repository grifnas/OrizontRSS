using System.Windows;
using System.Globalization;

namespace OrizontSetup;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var uninstallMode = e.Args.Any(a => string.Equals(a, "/uninstall", StringComparison.OrdinalIgnoreCase));
        var defaultLanguage = InstallerLanguages.ResolveCode(CultureInfo.CurrentUICulture.Name);
        var selectedLanguage = defaultLanguage;
        if (!uninstallMode)
        {
            var languageWindow = new LanguageWindow(defaultLanguage);
            if (languageWindow.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
            selectedLanguage = languageWindow.SelectedCode;
        }

        MainWindow = new MainWindow(uninstallMode, selectedLanguage);
        MainWindow.Show();
    }
}
