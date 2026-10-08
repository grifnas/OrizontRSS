using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Services.Content;

namespace CititorRSS.Jaws;

public partial class ArticleTranslationWindow : Window
{
    private readonly string _articleTitle;
    private readonly string _articleLink;
    private readonly string _translationProvider;
    private readonly string _originalText;
    private readonly string _translationLanguage;

    public ArticleTranslationWindow(string translatedText)
        : this(translatedText, string.Empty, string.Empty, "Google Translate", string.Empty, null)
    {
    }

    public ArticleTranslationWindow(string translatedText, string articleTitle, string articleLink, string translationProvider, string? originalText = null, string? translationLanguage = null)
    {
        InitializeComponent();
        TranslatedText.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(TranslatedText_PreviewKeyDown), true);
        _articleTitle = articleTitle;
        _articleLink = articleLink;
        _translationProvider = translationProvider;
        _originalText = originalText ?? string.Empty;
        _translationLanguage = translationLanguage ?? ArticleDistributionContext.LanguageNameForCode(null);
        var statusMessage = F("Traducerea articolului s-a încheiat. Furnizor: {0}. Limba rezultatului: {1}.", _translationProvider, _translationLanguage);
        Title = T("Traducerea articolului");
        EmailButton.Content = T("Distribuie prin e-mail");
        WhatsAppButton.Content = T("Distribuie prin WhatsApp");
        CopyButton.Content = T("Copiază articolul complet");
        CloseButton.Content = T("Închide");
        TranslatedText.SetValue(AutomationProperties.NameProperty, T("Traducerea articolului"));
        TranslatedText.SetValue(AutomationProperties.HelpTextProperty,
            $"{T("Folosește săgețile pentru a citi traducerea. Escape închide fereastra și revine la articol.")} {T("Tasta Application sau Shift+F10 deschide meniul contextual.")}");
        if (Resources["TranslationContextMenu"] is ContextMenu contextMenu)
        {
            contextMenu.SetValue(AutomationProperties.NameProperty, T("Comenzi cititor"));
            if (contextMenu.Items[0] is MenuItem copyMenu)
            {
                copyMenu.Header = T("Copiere și distribuire");
                var copyItems = copyMenu.Items.OfType<MenuItem>().ToList();
                copyItems[0].Header = T("Copiază selecția");
                copyItems[1].Header = T("Copiază articolul complet");
                copyItems[2].Header = T("Copiază adresa articolului");
                copyItems[3].Header = T("Distribuie prin e-mail");
                copyItems[4].Header = T("Distribuie prin WhatsApp");
            }
        }
        TranslatedText.Text = translatedText;
        TranslationStatus.Text = statusMessage;
        Loaded += (_, _) =>
        {
            TranslatedText.Focus();
            TranslatedText.CaretIndex = 0;
            TranslatedText.Select(0, 0);
            StatusAnnouncer.Set(TranslationStatus, statusMessage, TranslationStatusBar);
        };
    }

    private string T(string text) => UiText.Translate(text);
    private string F(string text, params object?[] arguments) => UiText.Format(text, arguments);

    private void TranslatedText_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = ContextMenuKeyboard.Normalize(e.Key, e.SystemKey);
        if (!ContextMenuKeyboard.IsMenuShortcut(key, e.KeyboardDevice.Modifiers)) return;
        OpenContextMenu();
        e.Handled = true;
    }

    private void OpenContextMenu()
    {
        if (Resources["TranslationContextMenu"] is not ContextMenu menu || menu.IsOpen) return;
        menu.PlacementTarget = TranslatedText;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
        menu.IsOpen = true;
    }

    private void TranslatedText_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (TranslatedText.Text.Length == 0) e.Handled = true;
    }

    private void TranslationContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu || menu.Items.Count == 0 || menu.Items[0] is not MenuItem copyMenu) return;
        var items = copyMenu.Items.OfType<MenuItem>().ToList();
        if (items.Count >= 5)
        {
            items[0].IsEnabled = !string.IsNullOrEmpty(TranslatedText.SelectedText);
            items[2].IsEnabled = !string.IsNullOrWhiteSpace(_articleLink);
        }
    }

    private void CopySelection_Click(object sender, RoutedEventArgs e)
    {
        var selection = TranslatedText.SelectedText;
        if (string.IsNullOrEmpty(selection))
        {
            StatusAnnouncer.Set(TranslationStatus, T("Nu există text selectat pentru copiere."), TranslationStatusBar);
            return;
        }
        Clipboard.SetText(selection);
        StatusAnnouncer.Set(TranslationStatus, T("Selecția a fost copiată în clipboard."), TranslationStatusBar);
    }

    private void CopyFromContextMenu_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(ShareText());
        StatusAnnouncer.Set(TranslationStatus, T("Articolul complet a fost copiat în clipboard."), TranslationStatusBar);
    }

    private void CopyLink_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_articleLink)) return;
        Clipboard.SetText(_articleLink);
        StatusAnnouncer.Set(TranslationStatus, T("Adresa articolului a fost copiată în clipboard."), TranslationStatusBar);
    }

    private ArticleDistributionContext DistributionContext() => new(
        _articleTitle,
        _originalText,
        TranslatedText.Text,
        _articleLink,
        _translationProvider,
        _translationLanguage);

    private string ShareText()
    {
        var context = DistributionContext();
        var note = context.BuildTranslationNote(T("Traducere automată realizată prin {0} în limba {1}."));
        return ArticleSharing.BuildShareText(context, note, T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:"));
    }

    private void CopyTranslation_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(ShareText());
        MessageBox.Show(this, T("Articolul complet a fost copiat în clipboard."), T("Copiere și distribuire"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShareByEmail_Click(object sender, RoutedEventArgs e)
    {
        var shareText = ShareText();
        var context = DistributionContext();
        var note = context.BuildTranslationNote(T("Traducere automată realizată prin {0} în limba {1}."));
        var footer = ArticleSharing.BuildFooter(context, note, T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:"));
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
        var context = DistributionContext();
        var note = context.BuildTranslationNote(T("Traducere automată realizată prin {0} în limba {1}."));
        var footer = ArticleSharing.BuildFooter(context, note, T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:"));
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
