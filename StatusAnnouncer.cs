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
                Raise(target);
                if (container is not null) Raise(container);
            }
            catch (InvalidOperationException)
            {
                // Some screen-reader providers do not expose live-region events.
            }
        }));
    }

    private static void Raise(FrameworkElement element)
    {
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(element);
        peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
}
