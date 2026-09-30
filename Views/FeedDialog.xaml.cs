using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class FeedDialog : Window
{
    private List<string> _folders = ["Neorganizate"];
    private bool _settingFolders;
    public string FeedName => NameBox.Text.Trim();
    public string FeedUrl => UrlBox.Text.Trim();
    public string FolderName => FolderBox.SelectedItem is FolderChoice choice && !choice.IsCreate ? choice.Name : "Neorganizate";
    public string SelectedArticleOpenMode => ArticleOpenModeIds.Normalize((ArticleOpenMode.SelectedItem as ComboBoxItem)?.Tag?.ToString());
    public FeedDialog()
    {
        InitializeComponent();
        ApplyText();
        ArticleOpenMode.SelectedIndex = 0;
        SetFolders([]);
        Loaded += (_, _) => NameBox.Focus();
    }
    public FeedDialog(Feed feed) : this()
    {
        Title = T("Editează feed"); Heading.Text = T("Editează feedul RSS"); SaveButton.Content = T("Salvează");
        NameBox.Text = feed.Name; UrlBox.Text = feed.Url; FolderBox.Text = feed.Folder;
        SelectArticleOpenMode(feed.ArticleOpenMode);
    }
    public void SetValues(string name, string url, string folder)
    {
        NameBox.Text = name;
        UrlBox.Text = url;
        SetFolders(_folders.Append(folder));
        SelectFolder(folder);
    }
    public void SetFolders(IEnumerable<string> folders)
    {
        var selected = FolderName;
        _folders = folders.Append("Neorganizate")
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Select(folder => folder.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(folder => folder == "Neorganizate" ? string.Empty : folder, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        _settingFolders = true;
        FolderBox.ItemsSource = _folders.Select(folder => new FolderChoice(folder, false)).Append(new FolderChoice(T("Creează folder nou…"), true)).ToList();
        SelectFolder(selected);
        _settingFolders = false;
    }
    private void FolderBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_settingFolders || FolderBox.SelectedItem is not FolderChoice { IsCreate: true }) return;
        var dialog = new FolderNameDialog { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            SetFolders(_folders.Append(dialog.FolderName));
            SelectFolder(dialog.FolderName);
        }
        else SelectFolder("Neorganizate");
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FeedName) || !Uri.TryCreate(FeedUrl, UriKind.Absolute, out var address) || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps))
        {
            MessageBox.Show(this, T("Feedul nu poate fi salvat. Completează numele și introdu o adresă RSS sau Atom validă, care începe cu http:// sau https://."), T("Feed incomplet"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }
    private void SelectFolder(string? folder)
    {
        FolderBox.SelectedItem = FolderBox.Items.OfType<FolderChoice>().FirstOrDefault(item => !item.IsCreate && string.Equals(item.Name, folder, StringComparison.CurrentCultureIgnoreCase))
            ?? FolderBox.Items.OfType<FolderChoice>().First(item => !item.IsCreate && item.Name == "Neorganizate");
    }
    private void SelectArticleOpenMode(string? mode)
    {
        var normalized = ArticleOpenModeIds.Normalize(mode);
        ArticleOpenMode.SelectedItem = ArticleOpenMode.Items.OfType<ComboBoxItem>().FirstOrDefault(item => string.Equals(item.Tag?.ToString(), normalized, StringComparison.OrdinalIgnoreCase))
            ?? ArticleOpenMode.Items[0];
    }
    private void ApplyText()
    {
        Title = T("Adaugă feed");
        Heading.Text = T("Adaugă un feed RSS");
        ArticleOpenModeLabel.Content = T("La deschiderea articolului");
        AutomationProperties.SetName(ArticleOpenMode, T("La deschiderea articolului"));
        ArticleOpenMode.Items[0] = new ComboBoxItem { Content = T("Folosește setarea generală"), Tag = ArticleOpenModeIds.Default };
        ArticleOpenMode.Items[1] = new ComboBoxItem { Content = T("Mod standard în fereastra principală"), Tag = ArticleOpenModeIds.Standard };
        ArticleOpenMode.Items[2] = new ComboBoxItem { Content = T("Cititor Orizont"), Tag = ArticleOpenModeIds.Orizont };
        ArticleOpenModeNotice.Text = T("Alege unde se deschid articolele acestui feed când apeși Enter sau alegi Deschide și citește.");
    }
    private static string T(string source) => UiText.Translate(source);
    private sealed record FolderChoice(string Name, bool IsCreate)
    {
        public override string ToString() => Name;
    }
}
