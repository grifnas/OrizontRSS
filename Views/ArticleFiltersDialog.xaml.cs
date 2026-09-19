using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class ArticleFiltersDialog : Window
{
    public string SelectedState => (StateFilter.SelectedItem as FilterChoice)?.Key ?? "All";
    public string SelectedPeriod => (TimeFilter.SelectedItem as FilterChoice)?.Key ?? "Anytime";
    public string SelectedTag => (TagFilter.SelectedItem as FilterChoice)?.Key ?? "Toate etichetele";

    public ArticleFiltersDialog(string state, string period, string tag, IEnumerable<string> availableTags)
    {
        InitializeComponent();
        var states = new[]
        {
            new FilterChoice("All", UiText.Translate("Toate")),
            new FilterChoice("Unread", UiText.Translate("Necitite")),
            new FilterChoice("Favorites", UiText.Translate("Favorite")),
            new FilterChoice("ReadLater", UiText.Translate("De citit mai târziu"))
        };
        var periods = new[]
        {
            new FilterChoice("Anytime", UiText.Translate("Oricând")),
            new FilterChoice("Today", UiText.Translate("Astăzi")),
            new FilterChoice("Last24Hours", UiText.Translate("Ultimele 24 de ore")),
            new FilterChoice("Last7Days", UiText.Translate("Ultimele 7 zile")),
            new FilterChoice("Last30Days", UiText.Translate("Ultimele 30 de zile"))
        };
        StateFilter.ItemsSource = states;
        TimeFilter.ItemsSource = periods;
        var tags = availableTags
            .Select(value => new FilterChoice(value, value))
            .Prepend(new FilterChoice("Toate etichetele", UiText.Translate("Toate etichetele")))
            .Append(new FilterChoice(tag, tag == "Toate etichetele" ? UiText.Translate(tag) : tag))
            .Where(value => !string.IsNullOrWhiteSpace(value.Key))
            .DistinctBy(value => value.Key, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        TagFilter.ItemsSource = tags;
        StateFilter.SelectedItem = states.FirstOrDefault(value => string.Equals(value.Key, NormalizeState(state), StringComparison.OrdinalIgnoreCase)) ?? states[0];
        TimeFilter.SelectedItem = periods.FirstOrDefault(value => string.Equals(value.Key, NormalizePeriod(period), StringComparison.OrdinalIgnoreCase)) ?? periods[0];
        TagFilter.SelectedItem = tags.FirstOrDefault(value => string.Equals(value.Key, tag, StringComparison.CurrentCultureIgnoreCase)) ?? tags[0];
        Loaded += (_, _) => StateFilter.Focus();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        StateFilter.SelectedIndex = 0;
        TimeFilter.SelectedIndex = 0;
        TagFilter.SelectedIndex = 0;
        DialogStatus.Text = UiText.Translate("Filtrele au fost pregătite pentru resetare. Alege Aplică pentru confirmare sau Anulează pentru păstrarea valorilor anterioare.");
        StateFilter.Focus();
    }

    private void Apply_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private static string NormalizeState(string value) => value switch { "Toate" => "All", "Necitite" => "Unread", "Favorite" => "Favorites", "De citit mai târziu" => "ReadLater", _ => value };
    private static string NormalizePeriod(string value) => value switch { "Oricând" => "Anytime", "Astăzi" => "Today", "Ultimele 24 de ore" => "Last24Hours", "Ultimele 7 zile" => "Last7Days", "Ultimele 30 de zile" => "Last30Days", _ => value };
    private sealed record FilterChoice(string Key, string Label)
    {
        public override string ToString() => Label;
    }
}
