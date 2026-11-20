using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CititorRSS.Jaws;

/// <summary>
/// Anunță cititorului de ecran elementul nou ales cu săgețile într-o listă derulantă închisă.
/// Standardul UI Automation cere ca schimbarea valorii unei liste derulante să fie semnalată; în unele ferestre
/// (de exemplu cele deschise nemodal din player) JAWS nu primea semnalul și schimbarea rămânea tăcută.
/// Anunțul folosește aceeași notificare UIA ca bara de stare, pe care JAWS o citește.
/// </summary>
internal static class ComboBoxSelectionAnnouncer
{
    public static void Attach(ComboBox comboBox) =>
        comboBox.SelectionChanged += (_, _) => Announce(comboBox);

    private static void Announce(ComboBox comboBox)
    {
        // Numai pentru schimbări de la tastatură, cu lista închisă; deschisă, JAWS citește singur elementele.
        if (!comboBox.IsKeyboardFocusWithin || comboBox.IsDropDownOpen) return;
        var text = comboBox.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(text)) return;

        comboBox.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            // Anunțul se emite numai dacă utilizatorul este încă pe același control, lista e închisă
            // și valoarea afișată este încă cea anunțată; altfel ar rosti o opțiune deja depășită.
            if (!comboBox.IsKeyboardFocusWithin || comboBox.IsDropDownOpen) return;
            if (comboBox.SelectedItem?.ToString() != text) return;
            try
            {
                var peer = UIElementAutomationPeer.CreatePeerForElement(comboBox);
                peer?.RaiseNotificationEvent(
                    AutomationNotificationKind.ItemAdded,
                    AutomationNotificationProcessing.ImportantMostRecent,
                    text,
                    "ComboBoxSelectionActivity");
            }
            catch (InvalidOperationException)
            {
                // Unele medii nu procesează notificările UIA; lista rămâne utilizabilă și fără anunț.
            }
        }));
    }
}
