using System.Diagnostics;
using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class NewsBlurAuthWindow : Window
{
    private readonly AppSettings _settings;
    private readonly NewsBlurConnection _connection = new();

    public NewsBlurAuthWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        LogoutButton.IsEnabled = settings.NewsBlurConnected && !string.IsNullOrWhiteSpace(settings.EncryptedNewsBlurSession);
        CheckSessionButton.IsEnabled = true;
        if (settings.NewsBlurConnected && !string.IsNullOrWhiteSpace(settings.NewsBlurUsername))
            SetStatus(LoginStatus, LoginStatusBar, T("Cont NewsBlur conectat: {0}.", settings.NewsBlurUsername));
        Loaded += (_, _) => Username.Focus();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        await RunAsync(LoginButton, async () =>
        {
            var session = await _connection.LoginAsync(Username.Text, Password.Password);
            SaveSession(session);
            try
            {
                var feedCount = await _connection.GetFeedCountAsync(session.SessionId);
                SetStatus(LoginStatus, LoginStatusBar, feedCount is int count
                    ? T("Sesiunea NewsBlur este validă. NewsBlur a raportat {0} feeduri.", count)
                    : T("Autentificarea NewsBlur a reușit pentru {0}.", session.Username));
            }
            catch { SetStatus(LoginStatus, LoginStatusBar, T("Autentificarea NewsBlur a reușit pentru {0}.", session.Username)); }
            DialogResult = true;
        }, LoginStatus, LoginStatusBar);
    }

    private async void Signup_Click(object sender, RoutedEventArgs e)
    {
        if (!string.Equals(SignupPassword.Password, SignupConfirmation.Password, StringComparison.Ordinal))
        {
            SetStatus(SignupStatus, SignupStatusBar, T("Parolele nu coincid."));
            SignupConfirmation.Focus();
            return;
        }
        await RunAsync(SignupButton, async () =>
        {
            await _connection.SignupAsync(SignupUsername.Text, SignupEmail.Text, SignupPassword.Password);
            var session = await _connection.LoginAsync(SignupUsername.Text, SignupPassword.Password);
            SaveSession(session);
            try
            {
                var feedCount = await _connection.GetFeedCountAsync(session.SessionId);
                SetStatus(SignupStatus, SignupStatusBar, feedCount is int count
                    ? T("Contul NewsBlur a fost creat și autentificat pentru {0}. Feeduri disponibile: {1}.", session.Username, count)
                    : T("Contul NewsBlur a fost creat și autentificat pentru {0}.", session.Username));
            }
            catch { SetStatus(SignupStatus, SignupStatusBar, T("Contul NewsBlur a fost creat și autentificat pentru {0}.", session.Username)); }
            DialogResult = true;
        }, SignupStatus, SignupStatusBar);
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sessionId = SecretProtector.Unprotect(_settings.EncryptedNewsBlurSession);
            await _connection.LogoutAsync(sessionId);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, T("Deconectare NewsBlur"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        _settings.NewsBlurConnected = false;
        _settings.NewsBlurUsername = null;
        _settings.EncryptedNewsBlurSession = null;
        LogoutButton.IsEnabled = false;
        CheckSessionButton.IsEnabled = true;
        SetStatus(LoginStatus, LoginStatusBar, T("Contul NewsBlur a fost deconectat."));
        DialogResult = true;
    }

    private async void CheckSession_Click(object sender, RoutedEventArgs e)
    {
        await RunAsync(CheckSessionButton, async () =>
        {
            var sessionId = SecretProtector.Unprotect(_settings.EncryptedNewsBlurSession);
            var feedCount = await _connection.GetFeedCountAsync(sessionId);
            SetStatus(LoginStatus, LoginStatusBar, feedCount is int count
                ? T("Sesiunea NewsBlur este validă. NewsBlur a raportat {0} feeduri.", count)
                : T("Sesiunea NewsBlur este validă."));
        }, LoginStatus, LoginStatusBar);
    }

    private void OpenNewsBlur_Click(object sender, RoutedEventArgs e) => OpenUrl(NewsBlurConnection.ApiBaseUrl + "/login");
    private void OpenOAuthDocs_Click(object sender, RoutedEventArgs e) => OpenUrl(NewsBlurConnection.ApiBaseUrl + "/api#oauth");

    private void SaveSession(NewsBlurSession session)
    {
        _settings.NewsBlurConnected = true;
        _settings.NewsBlurUsername = session.Username;
        _settings.EncryptedNewsBlurSession = SecretProtector.Protect(session.SessionId);
    }

    private async Task RunAsync(System.Windows.Controls.Button button, Func<Task> operation, System.Windows.Controls.TextBlock status, FrameworkElement statusContainer)
    {
        button.IsEnabled = false;
        StatusAnnouncer.Set(status, T("Se comunică cu NewsBlur. Așteaptă."), statusContainer);
        try { await operation(); }
        catch (Exception exception) { StatusAnnouncer.Set(status, exception.Message, statusContainer); }
        finally { if (IsVisible) button.IsEnabled = true; }
    }

    private static void SetStatus(System.Windows.Controls.TextBlock status, FrameworkElement container, string message) =>
        StatusAnnouncer.Set(status, message, container);

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private static string T(string source, params object?[] args) => UiText.Format(source, args);
}
