using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed record KeywordArticleAlertItem(Article Article, string AccessibleText)
{
    public override string ToString() => AccessibleText;

    public static KeywordArticleAlertItem FromMatch(KeywordArticleMatch match) =>
        new(match.Article, UiText.Format("{0}. Cuvânt-cheie: {1}.", match.Article.Title, match.Keyword));
}

public partial class KeywordArticleAlertWindow : Window
{
    private readonly Func<Article, Task<bool>> _addToReadLater;

    public Article? ArticleToOpen { get; private set; }
    public bool ArticleStateChanged { get; private set; }

    public KeywordArticleAlertWindow(
        IReadOnlyList<KeywordArticleMatch> matches,
        Func<Article, Task<bool>> addToReadLater,
        bool isArchiveSearch = false)
    {
        InitializeComponent();
        _addToReadLater = addToReadLater;

        Title = UiText.Translate("Articole găsite după cuvintele-cheie");
        Heading.Text = Title;
        Instructions.Text = UiText.Format(
            isArchiveSearch
                ? "Sunt afișate primele {0} articole existente potrivite cu cuvintele-cheie, cel mult trei. Alege unul pentru a-l deschide sau a-l pune în Mai târziu."
                : "Sunt afișate primele {0} articole noi potrivite cu cuvintele-cheie, cel mult trei. Alege unul pentru a-l deschide sau a-l pune în Mai târziu.",
            matches.Count);
        AutomationProperties.SetName(ResultsList, UiText.Format("Rezultatele cuvintelor-cheie, listă. {0} rezultate.", matches.Count));
        AutomationProperties.SetHelpText(ResultsList, UiText.Translate("Folosește săgețile pentru a alege un articol. Enter îl deschide; Tab ajunge la acțiuni; Escape închide fereastra."));
        AutomationProperties.SetName(Feedback, UiText.Translate("Mesaj despre acțiunea pentru articol"));
        AutomationProperties.SetLiveSetting(Feedback, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(OpenArticleButton, UiText.Translate("Deschide articolul"));
        AutomationProperties.SetName(ReadLaterButton, UiText.Translate("Adaugă la Mai târziu"));
        AutomationProperties.SetName(CloseButton, UiText.Translate("Închide"));

        ResultsList.ItemsSource = matches
            .Select(KeywordArticleAlertItem.FromMatch)
            .ToList();
        if (ResultsList.Items.Count > 0) ResultsList.SelectedIndex = 0;

        Loaded += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            Activate();
            ResultsList.Focus();
            Keyboard.Focus(ResultsList);
        });
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var hasSelection = ResultsList.SelectedItem is KeywordArticleAlertItem;
        OpenArticleButton.IsEnabled = hasSelection;
        ReadLaterButton.IsEnabled = hasSelection;
    }

    private void ResultsList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || ResultsList.SelectedItem is not KeywordArticleAlertItem) return;
        OpenSelectedArticle();
        e.Handled = true;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        DialogResult = false;
        e.Handled = true;
    }

    private void OpenArticle_Click(object sender, RoutedEventArgs e) => OpenSelectedArticle();

    private void OpenSelectedArticle()
    {
        if (ResultsList.SelectedItem is not KeywordArticleAlertItem selected) return;
        ArticleToOpen = selected.Article;
        DialogResult = true;
    }

    private async void ReadLater_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is not KeywordArticleAlertItem selected) return;
        ReadLaterButton.IsEnabled = false;
        try
        {
            var added = await _addToReadLater(selected.Article);
            ArticleStateChanged |= added;
            Feedback.Text = UiText.Format(
                added ? "Articol adăugat la Mai târziu: {0}." : "Articolul este deja în lista Mai târziu: {0}.",
                selected.Article.Title);
        }
        catch (Exception exception)
        {
            Feedback.Text = UiText.Format("Nu s-a putut salva articolul pentru mai târziu: {0}", exception.Message);
        }
        finally
        {
            ReadLaterButton.IsEnabled = ResultsList.SelectedItem is KeywordArticleAlertItem;
            ResultsList.Focus();
            Keyboard.Focus(ResultsList);
        }
    }
}
