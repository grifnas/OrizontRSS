using System.Windows.Input;

namespace CititorRSS.Jaws;

internal static class ContextMenuKeyboard
{
    internal static Key Normalize(Key key, Key systemKey) => key == Key.System ? systemKey : key;

    internal static bool IsMenuShortcut(Key key, ModifierKeys modifiers) =>
        key == Key.Apps || key == Key.F10 && modifiers == ModifierKeys.Shift;

    // Fereastra cititorului tratează tastele de meniu înainte de caseta de text: tasta Aplicație nu mai este
    // lăsată casetei, care o trata separat și închidea meniul imediat după deschidere. WebView-ul este tratat de apelant.
    internal static bool ShouldHandleReaderWindowShortcut(Key key, ModifierKeys modifiers) =>
        IsMenuShortcut(key, modifiers);
}
