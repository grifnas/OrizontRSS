using System.Windows;

namespace OrizontSetup;

public partial class LanguageWindow : Window
{
    public string SelectedCode { get; private set; } = "ro-RO";
    private InstallerTexts CurrentTexts => InstallerLanguages.FromCode((LanguageComboBox.SelectedItem as LanguageChoice)?.Code ?? "ro-RO");

    public LanguageWindow(string defaultCode)
    {
        InitializeComponent();
        foreach (var texts in InstallerLanguages.All)
        {
            LanguageComboBox.Items.Add(new LanguageChoice(texts.Code, texts.DisplayName));
        }
        LanguageComboBox.SelectedItem = LanguageComboBox.Items.Cast<LanguageChoice>().FirstOrDefault(x => x.Code.Equals(defaultCode, StringComparison.OrdinalIgnoreCase))
            ?? LanguageComboBox.Items[0];
        ApplyTexts();
        Loaded += (_, _) => LanguageComboBox.Focus();
    }

    private void LanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => ApplyTexts();

    private void ApplyTexts()
    {
        if (HeadingText is null || LanguageComboBox.SelectedItem is not LanguageChoice) return;
        var texts = CurrentTexts;
        Title = texts.LanguageDialogTitle;
        HeadingText.Text = texts.LanguageDialogTitle;
        PromptText.Text = texts.LanguageDialogPrompt;
        ContinueButton.Content = texts.ContinueButton;
        CancelButton.Content = texts.CancelButton;
        ContinueButton.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, texts.ContinueButton);
        CancelButton.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, texts.CancelButton);
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedCode = (LanguageComboBox.SelectedItem as LanguageChoice)?.Code ?? "ro-RO";
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private sealed record LanguageChoice(string Code, string Name)
    {
        public override string ToString() => Name;
    }
}
