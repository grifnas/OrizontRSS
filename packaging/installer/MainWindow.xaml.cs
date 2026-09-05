using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
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
    private readonly InstallerTexts _texts;
    private int _lastAnnouncedProgress = -1;
    private readonly string _defaultInstallPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "Orizont RSS");

    public MainWindow(bool uninstallMode, string languageCode)
    {
        InitializeComponent();
        _uninstallMode = uninstallMode;
        _texts = InstallerLanguages.FromCode(languageCode);
        InstallPathBox.Text = _defaultInstallPath;
        ApplyTexts();
        Loaded += (_, _) =>
        {
            if (_uninstallMode)
            {
                Title = _texts.UninstallTitle;
                TitleText.Text = _texts.UninstallTitle;
                IntroText.Text = _texts.UninstallIntro;
                DataNoticeText.Text = _texts.UninstallDataNotice;
                InstallFolderGroup.Visibility = Visibility.Collapsed;
                DesktopShortcutCheckBox.Visibility = Visibility.Collapsed;
                BrowseButton.IsEnabled = false;
                PrimaryButton.Content = _texts.UninstallButton;
                PrimaryButton.SetValue(AutomationProperties.NameProperty, _texts.UninstallButton);
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
            Description = _texts.FolderHeader,
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
            SetStatus($"{_texts.FailedPrefix} {ex.Message}");
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
            throw new InvalidOperationException(_texts.FolderRequired);
        }

        Directory.CreateDirectory(destination);
        var tempZip = Path.Combine(Path.GetTempPath(), $"Orizont-RSS-{Version}.zip");
        SetStatus(_texts.Downloading);
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
                    var progress = received * 80d / total.Value;
                    ProgressBar.Value = progress;
                    var roundedProgress = Math.Clamp((int)(progress / 10) * 10, 0, 80);
                    if (roundedProgress != _lastAnnouncedProgress)
                    {
                        _lastAnnouncedProgress = roundedProgress;
                        SetStatus($"{_texts.Downloading} {roundedProgress}%");
                    }
                }
            }
        }
        catch
        {
            if (File.Exists(tempZip)) File.Delete(tempZip);
            throw;
        }

        SetStatus(_texts.Verifying);
        await using (var hashStream = File.OpenRead(tempZip))
        {
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(hashStream));
            if (!hash.Equals(ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(_texts.HashMismatch);
            }
        }

        SetStatus(_texts.Copying);
        ZipFile.ExtractToDirectory(tempZip, destination, overwriteFiles: true);
        var selfPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(selfPath))
        {
            File.Copy(selfPath, Path.Combine(destination, "OrizontSetup.exe"), overwrite: true);
        }

        SetStatus(_texts.CreatingShortcuts);
        CreateShortcuts(destination, DesktopShortcutCheckBox.IsChecked == true);
        ProgressBar.Value = 100;
        SetStatus(_texts.Completed);
        PrimaryButton.Content = _texts.StartButton;
        PrimaryButton.SetValue(AutomationProperties.NameProperty, _texts.StartButton);
        PrimaryButton.IsEnabled = true;
        PrimaryButton.Click -= PrimaryButton_Click;
        PrimaryButton.Click += (_, _) =>
        {
            Process.Start(new ProcessStartInfo(Path.Combine(destination, "Orizont.exe")) { UseShellExecute = true });
            Close();
        };
        CancelButton.Content = _texts.CloseButton;
        CancelButton.IsEnabled = true;
        File.Delete(tempZip);
    }

    private async Task UninstallAsync()
    {
        var destination = InstallPathBox.Text.TrimEnd(Path.DirectorySeparatorChar);
        if (!Directory.Exists(destination))
        {
            SetStatus(_texts.MissingFolder);
            Close();
            return;
        }

        var answer = System.Windows.MessageBox.Show(
            _texts.ConfirmMessage,
            _texts.ConfirmTitle,
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

        SetStatus(_texts.RemovingShortcuts);
        DeleteShortcuts();
        var currentSetup = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentSetup)) throw new InvalidOperationException(_texts.MissingInstaller);
        SetStatus(_texts.FinishingUninstall);
        var command = $"ping 127.0.0.1 -n 2 > nul & rmdir /s /q \"{destination}\"";
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c {command}")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetTempPath(),
            WindowStyle = ProcessWindowStyle.Hidden
        });
        await Task.Delay(300);
        Close();
    }

    private void CreateShortcuts(string destination, bool createDesktopShortcut)
    {
        var programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
        Directory.CreateDirectory(programs);
        CreateShortcut(Path.Combine(programs, "Orizont RSS.lnk"), Path.Combine(destination, "Orizont.exe"), destination, "Orizont RSS");
        CreateShortcut(Path.Combine(programs, "Dezinstalează Orizont RSS.lnk"), Path.Combine(destination, "OrizontSetup.exe"), destination, "Dezinstalează Orizont RSS", "/uninstall");
        if (createDesktopShortcut)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            CreateShortcut(Path.Combine(desktop, "Orizont RSS.lnk"), Path.Combine(destination, "Orizont.exe"), destination, "Orizont RSS");
        }
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
        var desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Orizont RSS.lnk");
        if (File.Exists(desktopShortcut)) File.Delete(desktopShortcut);
    }

    private void ApplyTexts()
    {
        var uninstall = _uninstallMode;
        Title = uninstall ? _texts.UninstallTitle : _texts.InstallTitle;
        TitleText.Text = uninstall ? _texts.UninstallTitle : _texts.InstallTitle;
        IntroText.Text = uninstall ? _texts.UninstallIntro : _texts.InstallIntro;
        InstallFolderGroup.Header = _texts.FolderHeader;
        BrowseButton.Content = _texts.BrowseButton;
        BrowseButton.SetValue(AutomationProperties.NameProperty, _texts.BrowseButton);
        DesktopShortcutCheckBox.Content = _texts.DesktopShortcut;
        DesktopShortcutCheckBox.SetValue(AutomationProperties.NameProperty, _texts.DesktopShortcut);
        DesktopShortcutCheckBox.SetValue(AutomationProperties.HelpTextProperty, _texts.DesktopShortcut);
        DataNoticeText.Text = uninstall ? _texts.UninstallDataNotice : _texts.DataNotice;
        PrimaryButton.Content = uninstall ? _texts.UninstallButton : _texts.InstallButton;
        CancelButton.Content = _texts.CancelButton;
        PrimaryButton.SetValue(AutomationProperties.NameProperty, PrimaryButton.Content);
        CancelButton.SetValue(AutomationProperties.NameProperty, _texts.CancelButton);
        InstallPathBox.SetValue(AutomationProperties.NameProperty, _texts.FolderHeader);
        StatusBarControl.SetValue(AutomationProperties.NameProperty, uninstall ? _texts.UninstallTitle : _texts.InstallTitle);
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
        StatusText.SetValue(AutomationProperties.NameProperty, $"{(_uninstallMode ? _texts.UninstallTitle : _texts.InstallTitle)}: {message}");
        if (UIElementAutomationPeer.CreatePeerForElement(StatusText) is FrameworkElementAutomationPeer peer)
        {
            peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }
}
