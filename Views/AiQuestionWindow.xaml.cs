using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class AiQuestionWindow : Window
{
    public string UserQuestion => Question.Text.Trim();
    public AiQuestionWindow(string providerName = "Gemini")
    {
        InitializeComponent();
        Title = UiText.Format("Întreabă {0} despre articol", providerName);
        QuestionTitle.Text = UiText.Translate("Întrebarea ta despre articol");
        QuestionIntro.Text = UiText.Format("Furnizorul {0} primește conținutul complet disponibil în panoul de lectură și răspunde în contextul articolului.", providerName);
        QuestionLabel.Content = UiText.Translate("Scrie întrebarea");
        Question.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, UiText.Format("Întrebarea pentru {0}", providerName));
        Question.SetValue(System.Windows.Automation.AutomationProperties.HelpTextProperty, UiText.Translate("Scrie întrebarea despre articol, apoi alege Trimite."));
        CancelButton.Content = UiText.Translate("Anulează");
        SendButton.Content = UiText.Translate("Trimite");
        Loaded += (_, _) => Question.Focus();
    }
    private void Send_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UserQuestion)) { MessageBox.Show(this, UiText.Translate("Întrebarea nu poate fi trimisă deoarece este goală. Scrie întrebarea despre articol și alege din nou Trimite."), UiText.Translate("Întrebarea lipsește"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        DialogResult = true;
    }
}
