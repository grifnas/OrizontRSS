using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed record NewsBlurDecisionOption(
    string Label,
    MessageBoxResult Result,
    bool IsDefault = false,
    bool IsCancel = false,
    bool OpensDuplicateCleanup = false);

public partial class NewsBlurDecisionWindow : Window
{
    public MessageBoxResult Decision { get; private set; } = MessageBoxResult.Cancel;
    public bool OpensDuplicateCleanup { get; private set; }

    public NewsBlurDecisionWindow(
        string title,
        string details,
        IEnumerable<NewsBlurDecisionOption> options)
    {
        InitializeComponent();
        Title = UiText.Translate(title);
        DecisionText.Text = UiText.Translate(details);
        AutomationProperties.SetHelpText(DecisionText, UiText.Translate("Folosește săgețile pentru a citi textul. Apasă Tab pentru a ajunge la opțiuni. Escape alege opțiunea sigură și închide dialogul."));

        foreach (var option in options)
        {
            var button = new Button
            {
                Content = UiText.Translate(option.Label),
                IsDefault = option.IsDefault,
                IsCancel = option.IsCancel,
                MinWidth = option.OpensDuplicateCleanup ? 190 : 90
            };
            AutomationProperties.SetName(button, UiText.Translate(option.Label));
            button.Click += (_, _) => Choose(option);
            OptionsPanel.Children.Add(button);
        }

        Loaded += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            Activate();
            DecisionText.Focus();
            Keyboard.Focus(DecisionText);
            DecisionText.CaretIndex = 0;
        });
    }

    private void Choose(NewsBlurDecisionOption option)
    {
        Decision = option.Result;
        OpensDuplicateCleanup = option.OpensDuplicateCleanup;
        DialogResult = option.Result == MessageBoxResult.Yes;
    }
}
