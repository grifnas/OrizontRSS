using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class ArticleRulesPreviewWindow : Window
{
    public List<(Article Article, List<string> Tags, bool Later)> Changes { get; } = [];
    public Article? RequestedArticle { get; private set; }
    public bool RequestContextMenu { get; private set; }
    private sealed record PreviewRow(Article Article, string Display, string Details)
    {
        public override string ToString() => Display;
    }
    public ArticleRulesPreviewWindow(List<ArticleRule> rules, List<Feed> feeds, bool allowApply)
    {
        InitializeComponent();
        UiLocalizer.Apply(this);
        var rows = new List<PreviewRow>();
        foreach (var feed in feeds)
        foreach (var article in feed.Articles)
        {
            var matching = rules.Where(r => ArticleRules.Matches(r, feed, article)).ToList();
            if (matching.Count == 0) continue;
            var tags = matching.SelectMany(r => r.Tags).Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Where(t => !(article.Tags ?? []).Contains(t, StringComparer.CurrentCultureIgnoreCase)).ToList();
            var later = !article.ReadLater && matching.Any(r => r.MarkReadLater);
            if (tags.Count == 0 && !later) continue;
            Changes.Add((article, tags, later));
            rows.Add(new PreviewRow(article, article.DisplayName, UiText.Format("Feed: {0}. Reguli: {1}. Etichete de adăugat: {2}. Mai târziu: {3}",
                feed.Name, string.Join(", ", matching.Select(r => r.Name)), string.Join(", ", tags),
                later ? UiText.Translate("Da") : UiText.Translate("Nu"))));
        }
        Summary.Text = UiText.Format("Articole care ar fi modificate: {0}.", Changes.Count);
        Results.ItemsSource = rows;
        Results.SelectedIndex = rows.Count > 0 ? 0 : -1;
        ApplyButton.Visibility = allowApply ? Visibility.Visible : Visibility.Collapsed;
        ApplyButton.IsEnabled = Changes.Count > 0;
        Loaded += (_, _) => Results.Focus();
    }
    private Article? SelectedArticle => (Results.SelectedItem as PreviewRow)?.Article;
    private void Results_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { OpenSelected(); e.Handled = true; }
        else if (e.Key == Key.Apps || (e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift))
        { e.Handled = true; RequestArticle(true); }
    }
    private void Results_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelected();
    private void Results_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        e.Handled = true;
        var row = ItemsControl.ContainerFromElement(Results, e.OriginalSource as DependencyObject) as ListBoxItem;
        if (row is not null) Results.SelectedItem = row.DataContext;
        RequestArticle(true);
    }
    private void OpenArticle_Click(object sender, RoutedEventArgs e) => OpenSelected();
    private void OpenSelected() => RequestArticle(false);
    private void RequestArticle(bool contextMenu)
    {
        if (PrepareArticleRequest(contextMenu)) DialogResult = true;
    }
    private bool PrepareArticleRequest(bool contextMenu)
    {
        if (SelectedArticle is not { } article) return false;
        RequestedArticle = article;
        RequestContextMenu = contextMenu;
        return true;
    }
    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, UiText.Format("Aplici modificările previzualizate pentru {0} articole și salvezi regulile?", Changes.Count),
            Title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes) DialogResult = true;
    }
}
