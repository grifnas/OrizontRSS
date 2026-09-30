using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class ArticleRuleDialog : Window
{
    public ArticleRule Rule { get; private set; }
    public ArticleRuleDialog(ArticleRule rule, List<Feed> feeds)
    {
        InitializeComponent();
        Rule = rule.Copy();
        RuleName.Text = Rule.Name;
        EnabledBox.IsChecked = Rule.Enabled;
        TermsBox.Text = string.Join(Environment.NewLine, Rule.Terms);
        AllTermsBox.IsChecked = Rule.MatchAll;
        ContentBox.IsChecked = Rule.IncludeContent;
        FeedList.ItemsSource = feeds;
        foreach (var feed in feeds.Where(f => Rule.FeedIds.Contains(f.Id))) FeedList.SelectedItems.Add(feed);
        AllFeedsBox.IsChecked = Rule.AllFeeds;
        var tags = feeds.SelectMany(f => f.Articles).SelectMany(a => a.Tags ?? []).Concat(Rule.Tags)
            .Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(t => t).ToList();
        TagList.ItemsSource = tags;
        foreach (var tag in tags.Where(t => Rule.Tags.Contains(t, StringComparer.CurrentCultureIgnoreCase))) TagList.SelectedItems.Add(tag);
        LaterBox.IsChecked = Rule.MarkReadLater;
        UiLocalizer.Apply(this);
        Loaded += (_, _) => RuleName.Focus();
    }
    private void Scope_Changed(object sender, RoutedEventArgs e)
    {
        if (FeedList is not null) FeedList.IsEnabled = AllFeedsBox.IsChecked != true;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var terms = TermsBox.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();
        var tags = TagList.SelectedItems.Cast<string>().Concat(TagsBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
        if (string.IsNullOrWhiteSpace(RuleName.Text) || terms.Count == 0 || terms.Count > 50 ||
            terms.Any(t => t.Length > 200) || tags.Any(t => t.Length > 50) ||
            (tags.Count == 0 && LaterBox.IsChecked != true) || (AllFeedsBox.IsChecked != true && FeedList.SelectedItems.Count == 0))
        {
            MessageBox.Show(this, UiText.Translate("Completează numele, expresiile, feedurile și cel puțin o acțiune. Maximum 50 de expresii a câte 200 de caractere și 50 de caractere per etichetă."),
                UiText.Translate("Regulă automată"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        Rule = new ArticleRule { Name = RuleName.Text.Trim(), Enabled = EnabledBox.IsChecked == true, Terms = terms,
            MatchAll = AllTermsBox.IsChecked == true, IncludeContent = ContentBox.IsChecked == true,
            AllFeeds = AllFeedsBox.IsChecked == true, FeedIds = FeedList.SelectedItems.Cast<Feed>().Select(f => f.Id).ToList(),
            Tags = tags, MarkReadLater = LaterBox.IsChecked == true };
        DialogResult = true;
    }
}
