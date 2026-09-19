using System.Diagnostics;
using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class ArticleTranslationWindow : Window
{
    private readonly string _articleTitle;
    private readonly string _articleLink;
    private readonly string _translationProvider;

    public ArticleTranslationWindow(string translatedText)
        : this(translatedText, string.Empty, string.Empty, "Google Translate")
    {
    }

    public ArticleTranslationWindow(string translatedText, string articleTitle, string articleLink, string translationProvider)
    {
        InitializeComponent();
        _articleTitle = articleTitle;
        _articleLink = articleLink;
        _translationProvider = translationProvider;
        Title = T("Traducerea articolului");
        EmailButton.Content = T("Distribuie prin e-mail");
        WhatsAppButton.Content = T("Distribuie prin WhatsApp");
        CloseButton.Content = T("Închide");
        TranslatedText.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, T("Traducerea articolului"));
        TranslatedText.Text = translatedText;
        Loaded += (_, _) =>
        {
            TranslatedText.Focus();
            TranslatedText.CaretIndex = 0;
            TranslatedText.Select(0, 0);
        };
    }

    private string T(string text) => UiText.Translate(text);
    private string F(string text, params object?[] arguments) => UiText.Format(text, arguments);

    private string ShareText() => ArticleSharing.BuildShareText(
        _articleTitle,
        TranslatedText.Text,
        _articleLink,
        F("Traducere automată realizată prin {0}.", _translationProvider),
        T("Conținut preluat prin Orizont RSS:"),
        T("Sursa articolului:"));

    private void ShareByEmail_Click(object sender, RoutedEventArgs e)
    {
        var shareText = ShareText();
        var footer = ArticleSharing.BuildFooter(T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:"), _articleLink, F("Traducere automată realizată prin {0}.", _translationProvider));
        var body = ArticleSharing.LimitForUri(shareText, ArticleSharing.EmailBodyLimit, footer);
        var truncated = !string.Equals(body, shareText, StringComparison.Ordinal);
        if (truncated) Clipboard.SetText(shareText);
        var mailto = ArticleSharing.CreateMailto(_articleTitle, body);
        try
        {
            Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
            MessageBox.Show(this,
                truncated ? F("{0} {1}", T("A fost deschisă aplicația de e-mail pentru distribuirea articolului."), T("Articolul complet a fost copiat în clipboard.")) : T("A fost deschisă aplicația de e-mail pentru distribuirea articolului."),
                T("Copiere și distribuire"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, F("Aplicația de e-mail nu a putut fi deschisă.\n\n{0}", exception.Message), T("Distribuire nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShareByWhatsApp_Click(object sender, RoutedEventArgs e)
    {
        var fullShareText = ShareText();
        var footer = ArticleSharing.BuildFooter(T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:"), _articleLink, F("Traducere automată realizată prin {0}.", _translationProvider));
        var shareText = ArticleSharing.LimitForUri(fullShareText, ArticleSharing.WhatsAppBodyLimit, footer);
        var truncated = !string.Equals(shareText, fullShareText, StringComparison.Ordinal);
        if (truncated) Clipboard.SetText(fullShareText);
        var address = ArticleSharing.CreateWhatsApp(shareText);
        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
            MessageBox.Show(this, truncated
                ? F("{0} {1}", T("WhatsApp a fost deschis pentru distribuirea articolului."), T("Articolul complet a fost copiat în clipboard."))
                : T("WhatsApp a fost deschis pentru distribuirea articolului."), T("Copiere și distribuire"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, F("WhatsApp nu a putut fi deschis: {0}", exception.Message), T("Distribuire nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
