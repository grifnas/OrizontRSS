using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace CititorRSS.Jaws.Localization;

public static class UiLocalizer
{
    public static void Apply(DependencyObject root)
    {
        var visited = new HashSet<DependencyObject>(ReferenceEqualityComparer.Instance);
        ApplyRecursive(root, visited);
    }

    private static void ApplyRecursive(DependencyObject current, HashSet<DependencyObject> visited)
    {
        if (!visited.Add(current)) return;

        if (current is Window window) window.Title = UiText.Translate(window.Title);
        if (current is TextBlock textBlock && !string.IsNullOrEmpty(textBlock.Text)) textBlock.Text = UiText.Translate(textBlock.Text);
        if (current is HeaderedContentControl headeredContent && headeredContent.Header is string contentHeader) headeredContent.Header = UiText.Translate(contentHeader);
        if (current is HeaderedItemsControl headeredItems && headeredItems.Header is string itemsHeader) headeredItems.Header = UiText.Translate(itemsHeader);
        if (current is ContentControl contentControl && contentControl.Content is string content) contentControl.Content = UiText.Translate(content);
        if (current is FrameworkElement element)
        {
            var name = AutomationProperties.GetName(element);
            if (!string.IsNullOrEmpty(name)) AutomationProperties.SetName(element, UiText.Translate(name));
            var help = AutomationProperties.GetHelpText(element);
            if (!string.IsNullOrEmpty(help)) AutomationProperties.SetHelpText(element, UiText.Translate(help));
            if (element.ToolTip is string toolTip) element.ToolTip = UiText.Translate(toolTip);
        }

        if (current is ItemsControl itemsControl)
            foreach (var item in itemsControl.Items.OfType<DependencyObject>()) ApplyRecursive(item, visited);

        foreach (var child in LogicalTreeHelper.GetChildren(current).OfType<DependencyObject>()) ApplyRecursive(child, visited);
    }
}
