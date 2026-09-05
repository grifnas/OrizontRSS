using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class TagDialog : Window
{
    public List<string> Tags { get; private set; } = [];
    public List<string> TagsToDelete { get; private set; } = [];
    public TagDialog(IEnumerable<string> selectedTags, IEnumerable<string> availableTags)
    {
        InitializeComponent();
        var selected = selectedTags.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
        var available = availableTags.Concat(selected).Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(tag => tag).ToList();
        ExistingTags.ItemsSource = available;
        foreach (var tag in available.Where(tag => selected.Contains(tag, StringComparer.CurrentCultureIgnoreCase))) ExistingTags.SelectedItems.Add(tag);
        Loaded += (_, _) => ExistingTags.Focus();
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var newTags = NewTagsBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Tags = ExistingTags.SelectedItems.Cast<string>().Concat(newTags)
            .Where(tag => tag.Length <= 50).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
        DialogResult = true;
    }
    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = ExistingTags.SelectedItems.Cast<string>().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, T("Nu a fost selectată nicio etichetă pentru ștergere. Alege una sau mai multe etichete existente și încearcă din nou."), T("Nicio etichetă selectată"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var names = string.Join(", ", selected);
        var message = selected.Count == 1
            ? F("Ștergi eticheta „{0}” din toate articolele?", names)
            : F("Ștergi etichetele {0} din toate articolele?", names);
        if (MessageBox.Show(this, message, T("Confirmă ștergerea etichetelor"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        TagsToDelete = selected;
        DialogResult = true;
    }
    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
