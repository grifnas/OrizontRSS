using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace OrizontSetup;

public partial class MainWindow : Window
{
    private const string Version = "1.5.3";
    private const string DownloadUrl = "https://github.com/grifnas/OrizontRSS/releases/download/v1.5.3/Orizont-RSS-1.5.3-win-x64.zip";
    private const string ExpectedSha256 = "7536070499680C2C2859C69C181BB8B6689EFA510E51C8E61E01DBD4EB48F6EB";
    private readonly bool _uninstallMode;
    private readonly string _defaultInstallPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "Orizont RSS");

    public MainWindow(bool uninstallMode)
    {
        InitializeComponent();
        _uninstallMode = uninstallMode;
        InstallPathBox.Text = _defaultInstallPath;
        Loaded += (_, _) =>
        {
            if (_uninstallMode)
            {
                Title = "Dezinstalare Orizont RSS";
                TitleText.Text = "Dezinstalare Orizont RSS";
                IntroText.Text = "Aplicația și scurtăturile Orizont RSS vor fi eliminate din acest folder.";
                DataNoticeText.Text = "Feedurile, articolele și setările din profilul Windows nu sunt șterse.";
                InstallFolderGroup.Visibility = Visibility.Collapsed;
                BrowseButton.IsEnabled = false;
                PrimaryButton.Content = "Dezinstalează";
                PrimaryButton.SetValue(AutomationProperties.NameProperty, "Dezinstalează Orizont RSS");
                InstallPathBox.Text = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
                PrimaryButton.Focus();
            }
            else
            {
                InstallPathBox.Focus();
                InstallPathBox.SelectAll();
            }
        };
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Alege folderul pentru Orizont RSS",
            SelectedPath = InstallPathBox.Text
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            InstallPathBox.Text = dialog.SelectedPath;
        }
    }

    private async void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        PrimaryButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        try
        {
            if (_uninstallMode)
            {
                await UninstallAsync();
            }
            else
            {
                await InstallAsync();
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Operația nu a reușit: {ex.Message}");
            PrimaryButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private async Task InstallAsync()
    {
        var destination = InstallPathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new InvalidOperationException("Alege un folder pentru instalare.");
        }

        Directory.CreateDirectory(destination);
        var tempZip = Path.Combine(Path.GetTempPath(), $"Orizont-RSS-{Version}.zip");
        SetStatus("Se descarcă pachetul Orizont RSS...");
        ProgressBar.Value = 0;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            using var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength;
            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = File.Create(tempZip);
            var buffer = new byte[1024 * 128];
            long received = 0;
            int read;
            while ((read = await input.ReadAsync(buffer)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read));
                received += read;
                if (total is > 0)
                {
                    ProgressBar.Value = received * 80d / total.Value;
                }
            }
        }
        catch
        {
            if (File.Exists(tempZip)) File.Delete(tempZip);
            throw;
        }

        SetStatus("Se verifică integritatea pachetului...");
        await using (var hashStream = File.OpenRead(tempZip))
        {
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(hashStream));
            if (!hash.Equals(ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Hash-ul pachetului descărcat nu corespunde Release-ului oficial.");
            }
        }

        SetStatus("Se copiază fișierele aplicației...");
        ZipFile.ExtractToDirectory(tempZip, destination, overwriteFiles: true);
        var selfPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(selfPath))
        {
            File.Copy(selfPath, Path.Combine(destination, "OrizontSetup.exe"), overwrite: true);
        }

        SetStatus("Se creează scurtăturile accesibile...");
        CreateShortcuts(destination);
        ProgressBar.Value = 100;
        SetStatus("Instalarea s-a finalizat cu succes.");
        PrimaryButton.Content = "Pornește Orizont RSS";
        PrimaryButton.SetValue(AutomationProperties.NameProperty, "Pornește Orizont RSS");
        PrimaryButton.IsEnabled = true;
        PrimaryButton.Click -= PrimaryButton_Click;
        PrimaryButton.Click += (_, _) => Process.Start(new ProcessStartInfo(Path.Combine(destination, "Orizont.exe")) { UseShellExecute = true });
        CancelButton.Content = "Închide";
        CancelButton.IsEnabled = true;
        File.Delete(tempZip);
    }

    private async Task UninstallAsync()
    {
        var destination = InstallPathBox.Text.TrimEnd(Path.DirectorySeparatorChar);
        if (!Directory.Exists(destination))
        {
            SetStatus("Folderul instalării nu mai există.");
            Close();
            return;
        }

        var answer = System.Windows.MessageBox.Show(
            "Sigur dorești să dezinstalezi Orizont RSS? Datele utilizatorului nu vor fi șterse.",
            "Confirmă dezinstalarea",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
        {
            SetStatus("Dezinstalarea a fost anulată.");
            PrimaryButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            return;
        }

        SetStatus("Se elimină scurtăturile...");
        DeleteShortcuts();
        var currentSetup = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentSetup)) throw new InvalidOperationException("Nu s-a putut determina instalatorul.");
        SetStatus("Se finalizează dezinstalarea...");
        var command = $"ping 127.0.0.1 -n 2 > nul & rmdir /s /q \"{destination}\"";
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c {command}")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });
        await Task.Delay(300);
        Close();
    }

    private void CreateShortcuts(string destination)
    {
        var programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
        Directory.CreateDirectory(programs);
        CreateShortcut(Path.Combine(programs, "Orizont RSS.lnk"), Path.Combine(destination, "Orizont.exe"), destination, "Orizont RSS");
        CreateShortcut(Path.Combine(programs, "Dezinstalează Orizont RSS.lnk"), Path.Combine(destination, "OrizontSetup.exe"), destination, "Dezinstalează Orizont RSS", "/uninstall");
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string description, string arguments = "")
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("Windows Script Host nu este disponibil.");
        dynamic shell = Activator.CreateInstance(shellType) ?? throw new InvalidOperationException("Nu s-a putut crea scurtătura.");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Arguments = arguments;
        shortcut.Description = description;
        shortcut.IconLocation = targetPath;
        shortcut.Save();
    }

    private static void DeleteShortcuts()
    {
        var programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
        foreach (var name in new[] { "Orizont RSS.lnk", "Dezinstalează Orizont RSS.lnk" })
        {
            var path = Path.Combine(programs, name);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
        if (UIElementAutomationPeer.CreatePeerForElement(StatusText) is FrameworkElementAutomationPeer peer)
        {
            peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }
}
