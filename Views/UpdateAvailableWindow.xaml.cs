using System.Diagnostics;
using System.Windows;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Services.Update;

namespace CititorRSS.Jaws.Views;

public partial class UpdateAvailableWindow : Window
{
    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] args) => UiText.Format(source, args);

    private readonly AppUpdateInfo _updateInfo;
    private CancellationTokenSource? _cts;

    public UpdateAvailableWindow(AppUpdateInfo updateInfo)
    {
        InitializeComponent();
        _updateInfo = updateInfo;

        UiLocalizer.Apply(this);

        HeaderTextBlock.Text = F("Este disponibilă o nouă versiune a aplicației ({0}). Versiunea ta curentă este {1}.", _updateInfo.LatestVersion, _updateInfo.CurrentVersion);
        ReleaseNotesTextBox.Text = string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes) ? T("Nu există note suplimentare pentru această versiune.") : _updateInfo.ReleaseNotes;

        Loaded += (_, _) => InstallButton.Focus();
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        InstallButton.IsEnabled = false;
        GitHubButton.IsEnabled = false;
        CloseButton.IsEnabled = false;
        DownloadProgressBar.Visibility = Visibility.Visible;
        StatusTextBlock.Text = T("Se pregătește descărcarea actualizării...");

        _cts = new CancellationTokenSource();
        var progress = new Progress<double>(percent =>
        {
            DownloadProgressBar.Value = percent;
            StatusTextBlock.Text = F("Se descarcă actualizarea... {0:0}%", percent);
        });

        try
        {
            await UpdateCheckerService.DownloadAndInstallAsync(_updateInfo.DownloadUrl, progress, _cts.Token);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = F("Descărcarea actualizării a eșuat: {0}", ex.Message);
            InstallButton.IsEnabled = true;
            GitHubButton.IsEnabled = true;
            CloseButton.IsEnabled = true;
            DownloadProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_updateInfo.ReleasePageUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo(_updateInfo.ReleasePageUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, F("Pagina GitHub nu a putut fi deschisă: {0}", ex.Message), T("Eroare"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        Close();
    }
}
