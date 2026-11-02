using System.IO;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Services.Rss;
using Microsoft.Win32;

namespace CititorRSS.Jaws;

public partial class YouTubeTranscriptWindow : Window
{
    private readonly YouTubeVideoSearchService.YouTubeVideoCandidate _video;
    private CancellationTokenSource? _loadCancellation;
    private bool _transcriptLoadedFromSelectedTrack;

    public YouTubeTranscriptWindow(YouTubeVideoSearchService.YouTubeVideoCandidate video)
    {
        _video = video;
        InitializeComponent();
        WindowStatusAnnouncer.Attach(this, () =>
            UiText.Translate("Transcript YouTube") + " " +
            UiText.Translate("Folosește Tab pentru a parcurge comenzile."));
        Loaded += OnLoaded;
        Closed += (_, _) => _loadCancellation?.Cancel();
    }

    // ───────────────────────────────────────────────
    // Inițializare
    // ───────────────────────────────────────────────

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        VideoTitleLabel.Text = _video.Title;
        StatusText.Text = T("Se încarcă limbile disponibile...");

        if (_loadCancellation is not null) return;
        var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;

        try
        {
            var tracks = await YouTubeTranscriptService.GetAvailableTracksAsync(_video.VideoId, cancellation.Token);
            TranscriptLanguage.Items.Clear();
            foreach (var track in tracks)
                TranscriptLanguage.Items.Add(track);
            TranscriptLanguage.DisplayMemberPath = nameof(YouTubeTranscriptService.TranscriptTrack.DisplayName);
            TranscriptLanguage.SelectedIndex = 0;
            TranscriptLanguage.IsEnabled = true;
            IncludeTimestamps.IsEnabled = true;
            LoadTranscriptButton.IsEnabled = true;
            StatusText.Text = F("{0} limbi de subtitrare disponibile. Selectează limba și apasă Importă transcript.", tracks.Count);
            TranscriptLanguage.Focus();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception)
        {
            StatusText.Text = T("Preluarea automată a transcriptului nu a reușit. Deschide videoclipul în browser, copiază transcriptul din YouTube și importă-l din clipboard.");
            LoadTranscriptButton.IsEnabled = false;
            TranscriptLanguage.IsEnabled = false;
            IncludeTimestamps.IsEnabled = false;
            OpenVideoButton.Focus();
        }
        finally
        {
            _loadCancellation = null;
            cancellation.Dispose();
        }
    }

    // ───────────────────────────────────────────────
    // Handlers controale
    // ───────────────────────────────────────────────

    private void TranscriptLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Dacă s-a schimbat limba și există deja un transcript, îl ștergem pentru a evita confuziile.
        if (_transcriptLoadedFromSelectedTrack && TranscriptText.Text.Length > 0)
        {
            TranscriptText.Clear();
            CopyButton.IsEnabled = false;
            SaveButton.IsEnabled = false;
            _transcriptLoadedFromSelectedTrack = false;
            StatusText.Text = T("Limbă schimbată. Apasă Importă transcript pentru a importa din nou.");
        }
    }

    private async void LoadTranscript_Click(object sender, RoutedEventArgs e)
    {
        if (TranscriptLanguage.SelectedItem is not YouTubeTranscriptService.TranscriptTrack track)
        {
            StatusText.Text = T("Selectează o limbă din lista de subtitrări.");
            TranscriptLanguage.Focus();
            return;
        }

        if (_loadCancellation is not null) return;
        var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;
        SetControlsBusy(true);
        StatusText.Text = T("Se importă transcriptul...");

        try
        {
            var includeTimestamps = IncludeTimestamps.IsChecked == true;
            var text = await YouTubeTranscriptService.GetTranscriptAsync(track.BaseUrl, includeTimestamps, cancellation.Token);
            TranscriptText.Text = text;
            _transcriptLoadedFromSelectedTrack = true;
            CopyButton.IsEnabled = text.Length > 0;
            SaveButton.IsEnabled = text.Length > 0;
            var lineCount = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            StatusText.Text = F("Transcript importat: {0} rânduri de text. Folosește Ctrl+A și Ctrl+C pentru a copia.", lineCount);
            TranscriptText.Focus();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception)
        {
            StatusText.Text = T("Preluarea automată a transcriptului nu a reușit. Deschide videoclipul în browser, copiază transcriptul din YouTube și importă-l din clipboard.");
            PasteTranscriptButton.Focus();
        }
        finally
        {
            _loadCancellation = null;
            cancellation.Dispose();
            SetControlsBusy(false);
        }
    }

    private void OpenVideo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(_video.Url) { UseShellExecute = true });
            StatusText.Text = T("Videoclipul se deschide în browserul implicit.");
        }
        catch (Exception exception)
        {
            StatusText.Text = F("Nu s-a putut deschide videoclipul în browser: {0}", exception.Message);
        }
    }

    private void PasteTranscript_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = Clipboard.GetText(TextDataFormat.UnicodeText);
            if (string.IsNullOrWhiteSpace(text))
            {
                StatusText.Text = T("Clipboardul nu conține text. Copiază transcriptul din YouTube și încearcă din nou.");
                PasteTranscriptButton.Focus();
                return;
            }

            TranscriptText.Text = text;
            _transcriptLoadedFromSelectedTrack = false;
            TranscriptLanguage.SelectedIndex = -1;
            TranscriptLanguage.IsEnabled = false;
            LoadTranscriptButton.IsEnabled = false;
            IncludeTimestamps.IsEnabled = false;
            CopyButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
            var lineCount = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
            StatusText.Text = F("Transcript importat din clipboard: {0} rânduri.", lineCount);
            TranscriptText.Focus();
        }
        catch (Exception exception)
        {
            StatusText.Text = F("Nu s-a putut citi clipboardul: {0}", exception.Message);
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var text = TranscriptText.Text;
        if (string.IsNullOrEmpty(text))
        {
            StatusText.Text = T("Nu există transcript de copiat.");
            return;
        }
        try
        {
            Clipboard.SetText(text);
            StatusText.Text = T("Transcriptul a fost copiat în clipboard.");
        }
        catch (Exception exception)
        {
            StatusText.Text = F("Copierea în clipboard nu a reușit: {0}", exception.Message);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var text = TranscriptText.Text;
        if (string.IsNullOrEmpty(text))
        {
            StatusText.Text = T("Nu există transcript de salvat.");
            return;
        }

        // Construim un nume de fișier implicit curat din titlul videoclipului.
        var safeTitle = string.Concat(_video.Title.Split(Path.GetInvalidFileNameChars()));
        if (safeTitle.Length > 80) safeTitle = safeTitle[..80].TrimEnd();

        var dialog = new SaveFileDialog
        {
            Title = T("Salvează transcript ca fișier text"),
            Filter = T("Fișier text (*.txt)|*.txt"),
            DefaultExt = ".txt",
            FileName = string.IsNullOrWhiteSpace(safeTitle) ? "transcript" : safeTitle
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            File.WriteAllText(dialog.FileName, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            StatusText.Text = F("Transcriptul a fost salvat în {0}.", System.IO.Path.GetFileName(dialog.FileName));
        }
        catch (Exception exception)
        {
            StatusText.Text = F("Salvarea transcriptului nu a reușit: {0}", exception.Message);
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        Close();
        e.Handled = true;
    }

    // ───────────────────────────────────────────────
    // Utilitare
    // ───────────────────────────────────────────────

    private void SetControlsBusy(bool busy)
    {
        LoadTranscriptButton.IsEnabled = !busy && TranscriptLanguage.Items.Count > 0;
        TranscriptLanguage.IsEnabled = !busy && TranscriptLanguage.Items.Count > 0;
        IncludeTimestamps.IsEnabled = !busy && TranscriptLanguage.Items.Count > 0;
        OpenVideoButton.IsEnabled = !busy;
        PasteTranscriptButton.IsEnabled = !busy;
        CopyButton.IsEnabled = !busy && TranscriptText.Text.Length > 0;
        SaveButton.IsEnabled = !busy && TranscriptText.Text.Length > 0;
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
