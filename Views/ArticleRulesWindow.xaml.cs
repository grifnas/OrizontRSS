using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class ArticleRulesWindow : Window
{
    public List<ArticleRule> Rules { get; }
    public Article? RequestedArticle { get; private set; }
    public bool RequestContextMenu { get; private set; }
    public List<(Article Article, List<string> Tags, bool Later)> PendingChanges { get; private set; } = [];
    private readonly List<Feed> _feeds;
    private readonly Func<List<ArticleRule>, Task>? _saveRules;
    private bool _saving;
    private sealed record Row(ArticleRule Rule)
    {
        public string Display => (Rule.Enabled ? UiText.Translate("Activată") : UiText.Translate("Dezactivată")) + ": " + Rule.Name;
        public override string ToString() => Display;
    }
    public ArticleRulesWindow(IEnumerable<ArticleRule> rules, List<Feed> feeds, Func<List<ArticleRule>, Task>? saveRules = null)
    {
        InitializeComponent();
        Rules = rules.Select(r => r.Copy()).ToList();
        _feeds = feeds;
        _saveRules = saveRules;
        Closing += (_, e) => { if (_saving) e.Cancel = true; };
        UiLocalizer.Apply(this);
        Refresh();
        Loaded += (_, _) => RuleList.Focus();
    }
    private ArticleRule? Selected => (RuleList.SelectedItem as Row)?.Rule;
    private void Refresh(int index = 0)
    {
        RuleList.ItemsSource = Rules.Select(r => new Row(r)).ToList();
        EmptyMessage.Visibility = Rules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RuleList.SelectedIndex = Rules.Count == 0 ? -1 : Math.Clamp(index, 0, Rules.Count - 1);
    }
    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ArticleRuleDialog(new(), _feeds) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var candidate = Rules.Select(r => r.Copy()).ToList();
        candidate.Add(dialog.Rule);
        await SaveChangeAsync(candidate, candidate.Count - 1);
    }
    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } rule) return;
        var index = Rules.IndexOf(rule);
        var dialog = new ArticleRuleDialog(rule, _feeds) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var candidate = Rules.Select(r => r.Copy()).ToList();
        candidate[index] = dialog.Rule;
        await SaveChangeAsync(candidate, index);
    }
    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } rule) return;
        if (MessageBox.Show(this, UiText.Translate("Ștergi regula selectată? Articolele rămân neschimbate."), Title,
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        await SaveChangeAsync(Rules.Where(r => r != rule).Select(r => r.Copy()).ToList(), 0);
    }
    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } rule) return;
        var index = Rules.IndexOf(rule);
        var candidate = Rules.Select(r => r.Copy()).ToList();
        candidate[index].Enabled = !candidate[index].Enabled;
        await SaveChangeAsync(candidate, index);
    }
    private async Task CommitRulesAsync(List<ArticleRule> candidate, int index)
    {
        if (_saveRules is null) throw new InvalidOperationException("Rule persistence is not configured.");
        await _saveRules(candidate.Select(r => r.Copy()).ToList());
        Rules.Clear();
        Rules.AddRange(candidate.Select(r => r.Copy()));
        Refresh(index);
    }
    private async Task SaveChangeAsync(List<ArticleRule> candidate, int index)
    {
        if (_saving) return;
        _saving = true;
        ((UIElement)Content).IsEnabled = false;
        try
        {
            await CommitRulesAsync(candidate, index);
            MessageBox.Show(this, UiText.Translate("Regulile au fost salvate."), Title,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, string.Format(UiText.Translate("Operația nu a fost finalizată: {0}"), exception.Message), Title,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _saving = false;
            ((UIElement)Content).IsEnabled = true;
            RuleList.Focus();
        }
    }
    private void Test_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } rule) return;
        var copy = rule.Copy(); copy.Enabled = true;
        var preview = new ArticleRulesPreviewWindow([copy], _feeds, false) { Owner = this };
        if (preview.ShowDialog() == true) ForwardArticleRequest(preview);
    }
    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        var preview = new ArticleRulesPreviewWindow(Rules, _feeds, true) { Owner = this };
        if (preview.ShowDialog() != true) return;
        if (ForwardArticleRequest(preview)) return;
        PendingChanges = preview.Changes;
        DialogResult = true;
    }
    private bool ForwardArticleRequest(ArticleRulesPreviewWindow preview)
    {
        if (preview.RequestedArticle is null) return false;
        RequestedArticle = preview.RequestedArticle;
        RequestContextMenu = preview.RequestContextMenu;
        DialogResult = true;
        return true;
    }
}
