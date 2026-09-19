using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CititorRSS.Jaws;

internal static class StatusAnnouncer
{
    public static void Set(TextBlock target, string text, FrameworkElement? container = null)
    {
        AutomationProperties.SetLiveSetting(target, AutomationLiveSetting.Polite);
        target.Text = text;
        if (container is not null) AutomationProperties.SetLiveSetting(container, AutomationLiveSetting.Polite);
        if (!target.IsLoaded) return;
        target.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            try
            {
                Raise(target, text);
                if (container is not null) Raise(container, text);
            }
            catch (InvalidOperationException)
            {
                // Some screen-reader providers do not expose live-region events.
            }
        }));
    }

    private static void Raise(FrameworkElement element, string text)
    {
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(element);
        if (peer is null) return;

        try
        {
            peer.RaiseNotificationEvent(
                AutomationNotificationKind.ActionCompleted,
                AutomationNotificationProcessing.ImportantMostRecent,
                text,
                "StatusNotificationActivity");
        }
        catch
        {
            // Fallback pentru medii sau cititoare care nu procesează încă RaiseNotificationEvent
        }

        try
        {
            peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
        catch (InvalidOperationException)
        {
            // Unele cititoare de ecran nu expun evenimente de live-region.
        }
    }
}
