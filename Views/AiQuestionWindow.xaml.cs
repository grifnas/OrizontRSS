using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class AiQuestionWindow : Window
{
    public string UserQuestion => Question.Text.Trim();
    public AiQuestionWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Question.Focus();
    }
    private void Send_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UserQuestion)) { MessageBox.Show(this, UiText.Translate("Întrebarea nu poate fi trimisă deoarece este goală. Scrie întrebarea despre articol și alege din nou Trimite."), UiText.Translate("Întrebarea lipsește"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        DialogResult = true;
    }
}
