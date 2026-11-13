using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CititorRSS.Jaws.Services;

/// <summary>
/// Pictograme decorative pentru butoanele cu acțiuni clare. Alegerea se face după identificatorul butonului (x:Name),
/// nu după text, pentru că textul se schimbă cu limba interfeței. Pictograma nu are nume și nu este focalizabilă:
/// cititorul de ecran citește numele butonului, neschimbat. Butoanele cu identificator care nu corespunde acțiunii
/// (de exemplu CancelButton, care trimite întrebarea) rămân fără pictogramă.
/// </summary>
public static class ButtonIcons
{
    private const string Login = "M1,3 H7 V5 H3 V11 H7 V13 H1 Z M6,7 H11 V5 L15,8 L11,11 V9 H6 Z";
    private const string Logout = "M8,1 H14 V15 H8 V13 H12 V3 H8 Z M1,7 H6 V4 L10,8 L6,12 V9 H1 Z";
    private const string Search = "M6,1 A5,5 0 1,0 6.01,1 Z M6,3 A3,3 0 1,1 5.99,3 Z M10,10 L15,15 L14,16 L9,11 Z";
    private const string Check = "M1,8 L3,6 L6,9 L13,2 L15,4 L6,13 Z";
    private const string Key = "M0,8 A4,4 0 1,0 8,8 A4,4 0 1,0 0,8 Z M4,6 A2,2 0 1,1 4,10 A2,2 0 1,1 4,6 Z M8,7 H15 V9 H14 V11 H12 V9 H8 Z";
    private const string Play = "M3,2 L14,8 L3,14 Z";
    private const string Pause = "M3,2 H6 V14 H3 Z M10,2 H13 V14 H10 Z";
    private const string Cancel = "M3,2 L8,7 L13,2 L14,3 L9,8 L14,13 L13,14 L8,9 L3,14 L2,13 L7,8 L2,3 Z";
    private const string Add = "M7,2 H9 V7 H14 V9 H9 V14 H7 V9 H2 V7 H7 Z";
    private const string Copy = "M1,1 H10 V4 H7 V12 H1 Z M6,6 H15 V15 H6 Z";
    private const string Send = "M1,7 L15,1 L11,15 L8,10 Z";
    private const string Open = "M1,3 H6 L8,5 H15 V14 H1 Z";
    private const string Web = "M9,1 H15 V7 L13,5 L9,9 L7,7 L11,3 Z M1,4 H7 V6 H3 V13 H10 V9 H12 V15 H1 Z";
    private const string More = "M7,2 H9 V7 H14 V9 H9 V14 H7 V9 H2 V7 H7 Z";
    private const string Delete = "M5,1 H11 V3 H15 V5 H1 V3 H5 Z M3,6 H13 V15 H3 Z";
    private const string Save = "M1,1 H12 L15,4 V15 H1 Z M4,9 H12 V15 H4 Z";
    private const string Download = "M6.5,1 H9.5 V7 H12.5 L8,12 L3.5,7 H6.5 Z M2,13 H14 V15 H2 Z";
    private const string List = "M2,3 H4 V5 H2 Z M6,3 H15 V5 H6 Z M2,7 H4 V9 H2 Z M6,7 H15 V9 H6 Z M2,11 H4 V13 H2 Z M6,11 H15 V13 H6 Z";

    private static readonly IReadOnlyDictionary<string, string> IconsByName = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["LoginButton"] = Login,
        ["LogoutButton"] = Logout,
        ["SearchButton"] = Search,
        ["CatalogSearchButton"] = Search,
        ["LiveSearchButton"] = Search,
        ["PodcastSearchButton"] = Search,
        ["YouTubeSearchButton"] = Search,
        ["VerifyFeedButton"] = Check,
        ["CheckSessionButton"] = Check,
        ["CheckDeepLBalanceButton"] = Check,
        ["CheckDeepSeekBalanceButton"] = Check,
        ["TestDeepLButton"] = Check,
        ["TestGeminiButton"] = Check,
        ["TestOpenAiButton"] = Check,
        ["TestSoundButton"] = Check,
        ["TestSpeechButton"] = Check,
        ["MarkAllDisplayedReadButton"] = Check,
        ["GetDeepLKeyButton"] = Key,
        ["GetGeminiKeyButton"] = Key,
        ["GetOpenAiKeyButton"] = Key,
        ["SpeakConversationButton"] = Play,
        ["PauseSpeechButton"] = Pause,
        ["StopSpeechButton"] = Cancel,
        ["StopSpeechTestButton"] = Cancel,
        ["CloseButton"] = Cancel,
        ["AddFeedButton"] = Add,
        ["SubscribeChannelButton"] = Add,
        ["SignupButton"] = Add,
        ["CopyButton"] = Copy,
        ["CopyResponseDocumentButton"] = Copy,
        ["EmailButton"] = Send,
        ["EmailResponseButton"] = Send,
        ["WhatsAppButton"] = Send,
        ["WhatsAppResponseButton"] = Send,
        ["SendButton"] = Send,
        ["SendFollowUpButton"] = Send,
        ["OpenButton"] = Open,
        ["OpenInAppButton"] = Open,
        ["ReadNowButton"] = Open,
        ["AttentionToggleButton"] = Open,
        ["OpenResultPageButton"] = Web,
        ["OpenVideoButton"] = Web,
        ["GitHubButton"] = Web,
        ["LoadMoreButton"] = More,
        ["CleanupButton"] = Delete,
        ["ApplyButton"] = Save,
        ["ArticleFiltersButton"] = List,
        ["ImportTranscriptButton"] = Download,
    };

    /// <summary>Pune pictogramele pe butoanele fereastrei, după identificatorul lor. Butoanele fără pictogramă rămân neschimbate.</summary>
    public static void ApplyTo(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is Button { Content: string text } button
                && button.Name is { Length: > 0 } name
                && IconsByName.TryGetValue(name, out var geometry))
            {
                Apply(button, geometry, text);
            }
            ApplyTo(child);
        }
    }

    private static void Apply(Button button, string geometry, string text)
    {
        // Numele accesibil rămâne textul tradus, fără tasta de acces, cum era înainte.
        if (string.IsNullOrEmpty(AutomationProperties.GetName(button)))
            AutomationProperties.SetName(button, text.Replace("_", string.Empty));

        var icon = new Path
        {
            Data = Geometry.Parse(geometry),
            Width = 16,
            Height = 16,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        icon.SetResourceReference(Shape.FillProperty, "ThemeControlTextBrush");

        var label = new AccessText { Text = text, VerticalAlignment = VerticalAlignment.Center };
        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(icon);
        content.Children.Add(label);
        button.Content = content;
    }
}
