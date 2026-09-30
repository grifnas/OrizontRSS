using System.Windows;
using CititorRSS.Jaws.Services.Content;

namespace CititorRSS.Jaws;

public partial class MainWindow
{
    private long _contentRequestRevision;
    internal ArticleCommandPorts ArticleCommands { get; set; } = null!;

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
        ShowReader = (article, text, refresh) => new ArticleReaderWindow(
            article, text, _settings, refresh,
            () => _store.SaveSettingsAsync(_settings), _speech!,
            () => _store.SaveAsync(_feeds)) { Owner = this }.Show(),
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
