using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

/// <summary>Alege o valoare de la minim la maxim, cu pas fix (viteză sau ton pentru podcast).</summary>
public partial class PodcastRateDialog : Window
{
    public double Value { get; private set; }

    public PodcastRateDialog(string title, double minimum, double maximum, double step, double current)
    {
        InitializeComponent();
        Title = UiText.Translate(title);
        RateLabel.Content = UiText.Translate("Valoare");
        ApplyButton.Content = UiText.Translate("Aplică");
        CancelButton.Content = UiText.Translate("Anulează");
        WindowStatusAnnouncer.Attach(this, () => Title + " " + UiText.Translate("Alege valoarea din listă și apasă Aplică."));
        var steps = step > 0 && maximum > minimum ? (int)Math.Round((maximum - minimum) / step) : 0;
        for (var i = 0; i <= steps; i++)
        {
            var value = Math.Round(minimum + i * step, 2);
            RateBox.Items.Add(new ComboBoxItem { Content = value.ToString("F2", CultureInfo.CurrentCulture), Tag = value });
        }
        if (RateBox.Items.Count == 0) RateBox.Items.Add(new ComboBoxItem { Content = current.ToString("F2", CultureInfo.CurrentCulture), Tag = current });
        RateBox.SelectedItem = RateBox.Items.Cast<ComboBoxItem>().OrderBy(item => Math.Abs((double)item.Tag - current)).First();
        Loaded += (_, _) => RateBox.Focus();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (RateBox.SelectedItem is not ComboBoxItem item) return;
        Value = (double)item.Tag;
        DialogResult = true;
    }
}
