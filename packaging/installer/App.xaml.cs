using System.Windows;
using System.Globalization;
using System.IO;

namespace OrizontSetup;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            WriteStartupError(args.Exception);
            args.Handled = true;
            Shutdown(1);
        };

        try
        {
            base.OnStartup(e);
            // Keep the application alive while the language dialog is replaced
            // by the main installer window.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
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
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        catch (Exception ex)
        {
            WriteStartupError(ex);
            Shutdown(1);
        }
    }

    private static void WriteStartupError(Exception exception)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "OrizontSetup-startup.log");
            File.AppendAllText(path, $"[{DateTime.Now:O}] {exception}\r\n");
        }
        catch
        {
            // Logging must never mask the original startup failure.
        }
    }
}
