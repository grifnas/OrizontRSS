using System.Windows;
using System.Windows.Controls;
using CititorRSS.Jaws.Services.Content;
using CititorRSS.Jaws.Services.Rss;

namespace CititorRSS.Jaws;

public partial class MainWindow
{
    private long _contentRequestRevision;
    internal ArticleCommandPorts ArticleCommands { get; set; } = null!;

    /// <summary>
    /// Articolele cu adresă de videoclip YouTube, inclusiv din canalele la care ești abonat, se deschid în fereastra de videoclip Orizont.
    /// Întoarce false pentru orice alt articol, care rămâne pe calea obișnuită.
    /// </summary>
    private async Task<bool> TryOpenYouTubeVideoAsync(Article article)
    {
        if (!YouTubeCommon.TryGetVideoId(article.Link, out var videoId)) return false;
        try
        {
            new YouTubePlayerWindow(videoId, article.Title) { Owner = this }.Show();
            foreach (var copy in RelatedArticleCopies([article])) copy.IsRead = true;
            Articles.Items.Refresh();
            await ArticleCommands.SaveAsync();
            Say(T("Videoclipul se deschide în Orizont."));
        }
        catch (Exception exception)
        {
            Say(F("Nu s-a putut deschide videoclipul în Orizont: {0}", Describe(exception)));
        }
        return true;
    }

    /// <summary>Pentru videoclipuri YouTube afișează descrierea din feed, fără să descarce pagina de vizionare.</summary>
    private Task<string> LoadReadableForArticleAsync(Article article, string link)
    {
        if (!RssReader.IsVideoPlatformUrl(link)) return ArticleCommands.LoadReadableAsync(link);
        var note = T("Acesta este un videoclip. Afișăm descrierea din feed. Ca să-l urmărești, folosește „Deschide pagina originală în browser”.");
        var description = RssReader.CleanReadableContent(article.Content, article.Title);
        return Task.FromResult(string.IsNullOrWhiteSpace(description) ? note : note + "\n\n" + description);
    }

    /// <summary>La închiderea Cititorului, focusul revine pe articolul deschis, nu la începutul listei.</summary>
    private void FocusArticleAfterReader(Article article)
    {
        if (!Articles.Items.Contains(article)) { FocusArticles(); return; }
        Articles.SelectedItem = article;
        Articles.ScrollIntoView(article);
        Articles.UpdateLayout();
        if (Articles.ItemContainerGenerator.ContainerFromItem(article) is ListBoxItem row) row.Focus();
        else Articles.Focus();
    }

    private bool CanPresentArticleResult(Article article, long request) =>
        ReferenceEquals(_article, article) && request == _contentRequestRevision &&
        !_closeInProgress && !_isClosingAfterSave;

    private ArticleCommandPorts CreateArticleCommands() => new()
    {
        LoadReadableAsync = link => _rss.LoadReadableContentAsync(link),
        Unprotect = SecretProtector.Unprotect,
        LoadNewsBlurTextAsync = (session, hash) => new NewsBlurConnection().GetOriginalTextAsync(session, hash),
        TranslateDeepLAsync = (key, text, target) => new DeepLConnection().TranslateAsync(key, text, target),
        TranslateGoogleAsync = (text, target, source, cancellationToken) =>
            new GoogleTranslateConnection().TranslateAsync(text, target, source, cancellationToken),
        SaveAsync = () => _store.SaveAsync(_feeds),
        ShowReader = (article, text, refresh) =>
        {
            var reader = new ArticleReaderWindow(
                article, text, _settings, refresh,
                () => _store.SaveSettingsAsync(_settings), _speech!,
                () => _store.SaveAsync(_feeds),
                string.IsNullOrWhiteSpace(article.Link) ? null : () => _rss.LoadReadableHtmlAsync(article.Link)) { Owner = this };
            reader.Closed += (_, _) => FocusArticleAfterReader(article);
            reader.Show();
        },
        ShowTranslation = result =>
            new ArticleTranslationWindow(result.TranslatedText, result.Title, result.Link, result.Provider, result.OriginalText, result.Language) { Owner = this }.ShowDialog(),
        ConfirmGoogle = () => MessageBox.Show(this,
            T("Google Translate folosește un serviciu online neoficial care se poate schimba sau limita. Textul integral al articolului va fi trimis către Google. Continui?"),
            T("Traducere Google Translate"), MessageBoxButton.YesNo, MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes,
        ShowError = (message, title, icon) => MessageBox.Show(this, message, title, MessageBoxButton.OK, icon)
    };

    private ArticleDistributionContext CreateArticleDistributionContext(Article article, string? displayText)
    {
        var originalText = !string.IsNullOrWhiteSpace(article.FullContent) ? article.FullContent : article.Content;
        return new ArticleDistributionContext(article.Title, originalText ?? string.Empty, displayText ?? string.Empty,
            article.Link, _translationProvider, _translationLanguage);
    }
}
