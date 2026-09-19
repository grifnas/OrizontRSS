using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Globalization;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class StatusHistoryWindow : Window
{
    private readonly Action _clearHistory;

    public StatusHistoryWindow(IEnumerable<string> entries, Action clearHistory)
    {
        InitializeComponent();
        Title = $"{T("Istoric stare și erori")} — {AppVersionInfo.ProductTitle}";
        UiLocalizer.Apply(this);
        _clearHistory = clearHistory;
        HistoryText.Text = Format(entries);
        Loaded += (_, _) =>
        {
            HistoryText.Focus();
            HistoryText.CaretIndex = 0;
            HistoryText.Select(0, 0);
        };
    }

    private static string Format(IEnumerable<string> entries)
    {
        var text = string.Join(Environment.NewLine, entries);
        return string.IsNullOrWhiteSpace(text) ? T("Istoricul este gol.") : text;
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(HistoryText.Text);
        MessageBox.Show(this, T("Istoricul a fost copiat."), "Orizont RSS", MessageBoxButton.OK, MessageBoxImage.Information);
        HistoryText.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = T("Salvează diagnosticul Orizont RSS"),
            Filter = T("Fișier text (*.txt)|*.txt"),
            FileName = $"Orizont-diagnostic-{DateTime.Now:yyyy-MM-dd-HHmm}.txt",
            AddExtension = true,
            DefaultExt = ".txt",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;
        File.WriteAllText(dialog.FileName, $"{AppVersionInfo.ProductTitle}{Environment.NewLine}{DateTime.Now.ToString("G", CultureInfo.CurrentCulture)}{Environment.NewLine}{Environment.NewLine}{HistoryText.Text}");
        MessageBox.Show(this, T("Diagnosticul a fost salvat."), "Orizont RSS", MessageBoxButton.OK, MessageBoxImage.Information);
        HistoryText.Focus();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, T("Golești istoricul acestei sesiuni?"), T("Confirmă golirea"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        _clearHistory();
        HistoryText.Text = T("Istoricul este gol.");
        HistoryText.Focus();
        HistoryText.CaretIndex = 0;
    }
    private static string T(string source) => UiText.Translate(source);
}
