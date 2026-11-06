using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;
using Microsoft.Web.WebView2.Core;

namespace CititorRSS.Jaws;

/// <summary>
/// Redă un videoclip YouTube în Orizont, prin playerul oficial YouTube (IFrame API), într-o pagină locală.
/// Tastele Z X V B C S D sunt tratate în pagină, ca la podcast; tonul nu se poate schimba.
/// </summary>
public partial class YouTubePlayerWindow : Window
{
    private const string PlayerHost = "orizont-youtube.local";
    private static readonly Regex VideoIdPattern = new(@"^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant);
    private readonly string _videoId;
    private readonly string _watchUrl;
    private bool _initialized;

    public YouTubePlayerWindow(string videoId, string title)
    {
        _videoId = videoId ?? string.Empty;
        _watchUrl = $"https://www.youtube.com/watch?v={Uri.EscapeDataString(_videoId)}";
        InitializeComponent();
        Title = UiText.Format("Videoclip YouTube: {0}", title);
        WindowStatusAnnouncer.Attach(this, () => UiText.Translate("Videoclip YouTube") + " " +
            UiText.Translate("Videoclipul este gata. Alt+Z și Alt+X: înapoi, Alt+V și Alt+B: înainte, Alt+C: redare sau pauză, Alt+S: viteză, Alt+T: poziție, Alt+D: deschide în browser, Escape: închide."));
        Loaded += OnLoaded;
        // Închiderea ferestrei oprește și sunetul: WebView2 este eliberat, deci playerul nu mai rulează.
        Closed += (_, _) => PlayerWebView.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        if (!VideoIdPattern.IsMatch(_videoId))
        {
            StatusText.Text = UiText.Translate("Videoclipul nu are un identificator valid.");
            return;
        }

        try
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CititorRSS-JAWS");
            var userDataFolder = Path.Combine(root, "WebView2");
            var playerFolder = Path.Combine(root, "YouTubePlayer");
            Directory.CreateDirectory(userDataFolder);
            Directory.CreateDirectory(playerFolder);
            File.WriteAllText(Path.Combine(playerFolder, "player.html"), BuildPlayerPage(), new UTF8Encoding(false));

            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await PlayerWebView.EnsureCoreWebView2Async(environment);
            var core = PlayerWebView.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.SetVirtualHostNameToFolderMapping(PlayerHost, playerFolder, CoreWebView2HostResourceAccessKind.Allow);
            core.WebMessageReceived += OnWebMessageReceived;
            core.NavigationStarting += OnNavigationStarting;
            core.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                OpenInBrowser();
            };
            core.Navigate($"https://{PlayerHost}/player.html#{_videoId}");
            PlayerWebView.Focus();
        }
        catch (Exception exception)
        {
            StatusText.Text = UiText.Format("Playerul YouTube nu a putut porni: {0}", exception.Message);
        }
    }

    // Pagina locală rămâne singura încărcată în fereastră; orice altă adresă de nivel superior se deschide în browser.
    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) && uri.Host == PlayerHost) return;
        e.Cancel = true;
        OpenInBrowser();
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString() ?? string.Empty;
        if (message == "open") OpenInBrowser();
        else if (message == "close") Close();
        else if (message.StartsWith("status:", StringComparison.Ordinal)) StatusText.Text = message["status:".Length..];
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }

    private void OpenInBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo(_watchUrl) { UseShellExecute = true });
            StatusText.Text = UiText.Translate("Videoclipul se deschide în browser.");
        }
        catch (Exception exception)
        {
            StatusText.Text = UiText.Format("Nu s-a putut deschide videoclipul în browser: {0}", exception.Message);
        }
    }

    /// <summary>
    /// Pagina locală: încarcă IFrame API-ul oficial YouTube și leagă tastele la player.
    /// Textele vin din resurse, prin JSON, ca să fie în limba interfeței.
    /// </summary>
    private static string BuildPlayerPage()
    {
        var texts = new Dictionary<string, string>
        {
            ["ready"] = UiText.Translate("Videoclipul este gata. Alt+Z și Alt+X: înapoi, Alt+V și Alt+B: înainte, Alt+C: redare sau pauză, Alt+S: viteză, Alt+T: poziție, Alt+D: deschide în browser, Escape: închide."),
            ["unplayable"] = UiText.Translate("Videoclipul nu poate fi redat aici. Apasă D ca să-l deschizi în browser."),
            ["invalid"] = UiText.Translate("Videoclipul nu are un identificator valid."),
            ["back"] = UiText.Translate("Înapoi {0} secunde."),
            ["forward"] = UiText.Translate("Înainte {0} secunde."),
            ["playing"] = UiText.Translate("Redare."),
            ["paused"] = UiText.Translate("Pauză."),
            ["speed"] = UiText.Translate("Viteza de redare: {0}."),
            ["noPitch"] = UiText.Translate("Tonul nu se poate schimba la videoclipurile YouTube."),
            ["opening"] = UiText.Translate("Videoclipul se deschide în browser."),
            ["position"] = UiText.Translate("Poziția: {0} din {1}, adică {2}%. Mai sunt {3}."),
            ["locale"] = CultureInfo.CurrentUICulture.Name
        };
        // Evităm „</” în JSON-ul din script, ca textele să nu închidă elementul.
        var json = JsonSerializer.Serialize(texts).Replace("</", "<\\/", StringComparison.Ordinal);
        return PlayerPageTemplate
            .Replace("__TEXT__", json, StringComparison.Ordinal)
            .Replace("__LANG__", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, StringComparison.Ordinal);
    }

    private const string PlayerPageTemplate = """
<!doctype html>
<html lang="__LANG__">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="referrer" content="strict-origin-when-cross-origin">
<title>Orizont YouTube</title>
<style>
html, body { margin: 0; height: 100%; background: #000; color: #fff; font-family: "Segoe UI", sans-serif; }
#player { position: absolute; top: 0; left: 0; width: 100%; height: calc(100% - 56px); }
#status { position: absolute; left: 0; right: 0; bottom: 0; height: 56px; line-height: 56px; padding: 0 12px; background: #222; font-size: 16px; }
</style>
</head>
<body>
<div id="player"></div>
<div id="status" role="status" aria-live="polite"></div>
<script>
const TEXT = __TEXT__;
const VIDEO_ID = location.hash.slice(1);
let player = null;
let ready = false;

function say(text) {
  document.getElementById('status').textContent = text;
  window.chrome.webview.postMessage('status:' + text);
}

function fill(template, value) {
  return template.replace('{0}', String(value));
}

function onYouTubeIframeAPIReady() {
  if (!/^[A-Za-z0-9_-]{11}$/.test(VIDEO_ID)) {
    say(TEXT.invalid);
    return;
  }
  player = new YT.Player('player', {
    videoId: VIDEO_ID,
    playerVars: { playsinline: 1, rel: 0, origin: location.origin },
    events: {
      onReady: function () { ready = true; say(TEXT.ready); },
      onError: function () { say(TEXT.unplayable); }
    }
  });
}

function seek(delta) {
  const duration = player.getDuration() || Infinity;
  const target = Math.min(duration, Math.max(0, player.getCurrentTime() + delta));
  player.seekTo(target, true);
  say(fill(delta < 0 ? TEXT.back : TEXT.forward, Math.abs(delta)));
}

function togglePlay() {
  if (player.getPlayerState() === YT.PlayerState.PLAYING) {
    player.pauseVideo();
    say(TEXT.paused);
  } else {
    player.playVideo();
    say(TEXT.playing);
  }
}

function changeSpeed() {
  const rates = player.getAvailablePlaybackRates();
  const index = rates.indexOf(player.getPlaybackRate());
  const next = rates[(index + 1) % rates.length];
  player.setPlaybackRate(next);
  say(fill(TEXT.speed, next.toLocaleString(TEXT.locale)));
}

// Minutele și secundele se spun ca „5:03”; cu ore, ca „1:05:03”.
function clock(seconds) {
  const total = Math.floor(seconds);
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const secs = String(total % 60).padStart(2, '0');
  return hours > 0 ? hours + ':' + String(minutes).padStart(2, '0') + ':' + secs : minutes + ':' + secs;
}

function reportPosition() {
  const duration = player.getDuration();
  if (!(duration > 0)) return;
  const current = Math.min(duration, player.getCurrentTime());
  const percent = Math.round(current / duration * 100);
  const values = [clock(current), clock(duration), percent, clock(duration - current)];
  say(values.reduce(function (text, value, index) { return text.split('{' + index + '}').join(String(value)); }, TEXT.position));
}

// Comenzile sunt Alt+literă: JAWS folosește literele simple în modul de navigare și nu le trimite paginii.
document.addEventListener('keydown', function (event) {
  if (event.key === 'Escape') {
    if (player && player.stopVideo) player.stopVideo();
    window.chrome.webview.postMessage('close');
    return;
  }
  if (!event.altKey || event.ctrlKey || event.metaKey || !ready) return;
  switch (event.key.toLowerCase()) {
    case 'z': seek(-30); break;
    case 'x': seek(-5); break;
    case 'v': seek(5); break;
    case 'b': seek(30); break;
    case 'c': togglePlay(); break;
    case 's': changeSpeed(); break;
    case 't': reportPosition(); break;
    case 'p': say(TEXT.noPitch); break;
    case 'd': say(TEXT.opening); window.chrome.webview.postMessage('open'); break;
    default: return;
  }
  event.preventDefault();
});
</script>
<script src="https://www.youtube.com/iframe_api"></script>
</body>
</html>
""";
}
