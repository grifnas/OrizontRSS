using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded), true);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        UiCulture.Apply(FeedStore.LoadUiLanguageForStartup());
        base.OnStartup(e);
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window && ReferenceEquals(e.OriginalSource, window)) UiLocalizer.Apply(window);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var reportPath = TryWriteErrorReport(e.Exception);
        var location = string.IsNullOrWhiteSpace(reportPath)
            ? string.Empty
            : UiText.Format("\n\nUn raport tehnic a fost salvat în:\n{0}", reportPath);
        MessageBox.Show(
            UiText.Format("Orizont RSS a întâmpinat o eroare neașteptată și a oprit comanda curentă. Este posibil ca ultima modificare să nu fi fost salvată.\n\nÎnchide și redeschide aplicația înainte de a repeta operația.\n\nDetalii: {0}{1}", e.Exception.Message, location),
            UiText.Translate("Eroare neașteptată — Orizont RSS"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static string? TryWriteErrorReport(Exception exception)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CititorRSS-JAWS", "diagnostic");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"eroare-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt");
            var report = new StringBuilder()
                .AppendLine(UiText.Translate("Orizont RSS — raport de eroare neașteptată"))
                .AppendLine(UiText.Format("Moment: {0}", DateTimeOffset.Now.ToString("O")))
                .AppendLine(UiText.Format("Sistem: {0}", Environment.OSVersion))
                .AppendLine(UiText.Format("Proces pe 64 de biți: {0}", Environment.Is64BitProcess))
                .AppendLine()
                .AppendLine(exception.ToString())
                .ToString();
            File.WriteAllText(path, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            return path;
        }
        catch
        {
            return null;
        }
    }
}
