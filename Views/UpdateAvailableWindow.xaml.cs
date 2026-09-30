using System.Diagnostics;
using System.IO;
using System.Net.Http;
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
    private VerifiedUpdateInstaller? _preparedInstaller;

    public UpdateAvailableWindow(AppUpdateInfo updateInfo)
    {
        InitializeComponent();
        _updateInfo = updateInfo;

        UiLocalizer.Apply(this);

        HeaderTextBlock.Text = F("Este disponibilă o nouă versiune a aplicației ({0}). Versiunea ta curentă este {1}.", _updateInfo.LatestVersion, _updateInfo.CurrentVersion);
        ReleaseNotesTextBox.Text = string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes) ? T("Nu există note suplimentare pentru această versiune.") : _updateInfo.ReleaseNotes;

        Loaded += (_, _) => InstallButton.Focus();
        Closing += UpdateWindow_Closing;
    }

    public VerifiedUpdateInstaller? TakePreparedInstaller()
    {
        var installer = _preparedInstaller;
        _preparedInstaller = null;
        return installer;
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        InstallButton.IsEnabled = false;
        GitHubButton.IsEnabled = false;
        CloseButton.IsEnabled = true;
        DownloadProgressBar.Visibility = Visibility.Visible;
        StatusTextBlock.Text = T("Se pregătește descărcarea actualizării...");

        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;
        var progress = new Progress<double>(percent =>
        {
            if (!IsVisible) return;
            DownloadProgressBar.Value = percent;
            StatusTextBlock.Text = F("Se descarcă actualizarea... {0:0}%", percent);
        });

        try
        {
            _preparedInstaller = await UpdateCheckerService.DownloadAndVerifyAsync(_updateInfo, progress, cancellationToken);
            if (!IsVisible)
            {
                _preparedInstaller.Dispose();
                _preparedInstaller = null;
                return;
            }

            StatusTextBlock.Text = T("Actualizarea a fost descărcată și verificată. Se salvează datele înainte de pornirea instalatorului.");
            DialogResult = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (IsVisible)
            {
                StatusTextBlock.Text = T("Descărcarea actualizării a fost anulată.");
                InstallButton.IsEnabled = true;
                GitHubButton.IsEnabled = true;
                DownloadProgressBar.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            if (IsVisible)
            {
                Debug.WriteLine($"Update download failed: {ex}");
                StatusTextBlock.Text = F("Descărcarea actualizării a eșuat: {0}", DescribeDownloadFailure(ex));
                InstallButton.IsEnabled = true;
                GitHubButton.IsEnabled = true;
                DownloadProgressBar.Visibility = Visibility.Collapsed;
            }
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
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

    private static string DescribeDownloadFailure(Exception exception) => exception switch
    {
        HttpRequestException { StatusCode: not null } => exception.Message,
        HttpRequestException => T("Conexiunea la GitHub nu a putut fi stabilită."),
        OperationCanceledException => T("Descărcarea actualizării a expirat."),
        UnauthorizedAccessException => T("Nu există acces la folderul temporar al actualizării."),
        IOException => T("Fișierul temporar al actualizării nu a putut fi scris."),
        _ => exception.Message
    };

    private void UpdateWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DialogResult == true) return;
        _cts?.Cancel();
        _preparedInstaller?.Dispose();
        _preparedInstaller = null;
    }
}
