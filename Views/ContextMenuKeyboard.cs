using System.Windows.Input;

namespace CititorRSS.Jaws;

internal static class ContextMenuKeyboard
{
    internal static Key Normalize(Key key, Key systemKey) => key == Key.System ? systemKey : key;

    internal static bool IsMenuShortcut(Key key, ModifierKeys modifiers) =>
        key == Key.Apps || key == Key.F10 && modifiers == ModifierKeys.Shift;

    internal static bool ShouldHandleReaderWindowShortcut(Key key, ModifierKeys modifiers, bool webViewActive) =>
        IsMenuShortcut(key, modifiers) && (key != Key.Apps || webViewActive);
}
