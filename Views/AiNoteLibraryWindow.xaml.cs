using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class AiNoteLibraryWindow : Window
{
    private List<LibraryNote> _notes = [];
    public AiNoteLibraryWindow(IEnumerable<Feed> feeds)
    {
        InitializeComponent();
        _notes = feeds.SelectMany(feed => feed.Articles.SelectMany(article => (article.AiNotes ?? []).Select(note => new LibraryNote(feed.Name, article, note))))
            .OrderByDescending(entry => entry.Note.CreatedAt).ToList();
        RefreshList();
        Loaded += (_, _) => Search.Focus();
    }
    private void Search_TextChanged(object sender, TextChangedEventArgs e) => RefreshList();
    private void RefreshList()
    {
        var query = Search?.Text.Trim() ?? string.Empty;
        var visible = string.IsNullOrWhiteSpace(query) ? _notes : _notes.Where(note => $"{note.Article.Title} {note.FeedName} {note.Note.Title} {note.Note.Content}".Contains(query, StringComparison.CurrentCultureIgnoreCase));
        NotesList.ItemsSource = visible.ToList();
        if (NotesList.Items.Count > 0) NotesList.SelectedIndex = 0;
        else NoteContent?.Clear();
    }
    private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NotesList.SelectedItem is not LibraryNote entry) { NoteContent.Clear(); return; }
        NoteContent.Text = UiText.Format("Articol: {0}\nFeed: {1}\nSursă: {2}\nTip: {3}\nSalvat: {4}\n\n{5}", entry.Article.Title, entry.FeedName, entry.Article.Link, entry.Note.Title, entry.Note.CreatedAt.ToString("g", CultureInfo.CurrentCulture), entry.Note.Content);
        NoteContent.CaretIndex = 0;
    }
    private sealed record LibraryNote(string FeedName, Article Article, AiNote Note)
    {
        public string DisplayName => $"{Note.CreatedAt.ToString("g", CultureInfo.CurrentCulture)}. {Note.Title}. {Article.Title}";
        public string DisplayDate => $"{Note.CreatedAt.ToString("g", CultureInfo.CurrentCulture)} · {FeedName}";
    }
}
