using System.Windows;
using System.Globalization;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class AiNotesWindow : Window
{
    public AiNotesWindow(Article article)
    {
        InitializeComponent();
        Title = UiText.Format("Notițe AI: {0}", article.Title);
        Notes.Text = string.Join(
            $"{Environment.NewLine}{Environment.NewLine}{new string('─', 48)}{Environment.NewLine}{Environment.NewLine}",
            article.AiNotes.OrderByDescending(note => note.CreatedAt).Select(note => $"{note.Title} — {note.CreatedAt.ToString("g", CultureInfo.CurrentCulture)}{Environment.NewLine}{Environment.NewLine}{note.Content}"));
        Loaded += (_, _) => { Notes.Focus(); Notes.CaretIndex = 0; Notes.Select(0, 0); };
    }
}
