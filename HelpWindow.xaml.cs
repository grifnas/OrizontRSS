using System.Diagnostics;
using System.IO;
using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        // Translate before the window is shown so screen readers announce only
        // the title in the language selected by the user.
        UiLocalizer.Apply(this);
        ShortcutList.ItemsSource = Shortcuts();
        Loaded += (_, _) =>
        {
            ShortcutList.SelectedIndex = 0;
            ShortcutList.Focus();
        };
    }

    private static List<ShortcutEntry> Shortcuts() =>
    [
        new(T("Ajutor"), "F1", T("Deschide această listă de scurtături.")),
        new(T("Navigare"), T("Tab și Shift+Tab"), T("Trece la următorul sau precedentul control util, fără zone goale.")),
        new(T("Navigare"), T("F6 și Shift+F6"), T("Trece la următorul sau precedentul panou: Feeduri, Articole și Conținut articol.")),
        new(T("Navigare"), "Ctrl+1", T("Mută focalizarea în panoul Feeduri.")),
        new(T("Navigare"), "Ctrl+2", T("Mută focalizarea în lista Articole.")),
        new(T("Navigare"), "Ctrl+Shift+1", T("Mută focalizarea direct în selectorul de foldere.")),
        new(T("Navigare"), "Ctrl+Shift+2", T("Afișează lista combinată a articolelor din folderul selectat și mută focalizarea în ea.")),
        new(T("Navigare"), "Ctrl+3", T("Mută focalizarea în Conținut articol.")),
        new(T("Navigare"), "Ctrl+4", T("Deschide Citește acum: articole necitite, pentru mai târziu și favorite recente.")),
        new(T("Meniu"), "Alt sau F10", T("Activează meniul principal.")),
        new(T("Articole"), "Enter", T("Deschide conținutul articolului selectat.")),
        new(T("Articole"), "Escape", T("Revine din conținut, golește căutarea sau cere oprirea actualizării în curs.")),
        new(T("Ștergere"), "Delete", T("Șterge feedul, articolele selectate sau folderul, în funcție de controlul activ și după confirmare.")),
        new(T("Căutare"), "Ctrl+F sau F3", T("Mută focalizarea în căutarea articolelor.")),
        new(T("Filtrare"), "Ctrl+Shift+F", T("Deschide dialogul Filtre articole.")),
        new(T("Filtrare"), "Ctrl+Shift+U", T("Afișează toate articolele necitite din toate feedurile.")),
        new(T("Meniu contextual"), "Shift+F10", T("Deschide acțiunile disponibile pentru elementul curent.")),
        new(T("Selecție"), "Ctrl+A", T("Selectează toate articolele afișate.")),
        new(T("Articole"), "R", T("Marchează articolele selectate ca citite sau necitite.")),
        new(T("Articole"), "F", T("Adaugă sau elimină articolele selectate din Favorite.")),
        new(T("Articole"), "L", T("Adaugă sau elimină articolele selectate din De citit mai târziu.")),
        new(T("Conținut"), "Ctrl+Shift+R", T("Aduce textul complet al articolului de pe site.")),
        new(T("Stare"), "Ctrl+Shift+H", T("Deschide istoricul stării și al erorilor.")),
        new(T("Citire vocală"), "F9 / Ctrl+Alt+V", T("Citește cu motorul vocal selectat textul, articolul curent sau conversația Gemini.")),
        new(T("Citire vocală"), "F9 / Ctrl+Alt+P", T("Întrerupe sau continuă citirea vocală.")),
        new(T("Citire vocală"), "Escape / Ctrl+Alt+S", T("Oprește citirea vocală.")),
        new(T("Citire vocală"), "Shift+F9", T("Deschide Setări voce.")),
        new(T("Fereastră"), "F11", T("Maximizează sau restabilește fereastra.")),
    ];

    private void OpenGuide_Click(object sender, RoutedEventArgs e)
    {
        var path = UserGuideLocator.Find();
        if (path is null)
        {
            MessageBox.Show(this, T("Ghidul utilizatorului nu a putut fi găsit. Copiază din nou toate fișierele distribuției Orizont RSS."), T("Ghid indisponibil"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception exception) { MessageBox.Show(this, UiText.Format("Ghidul nu a putut fi deschis.\n\n{0}", exception.Message), T("Ghid indisponibil"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private static string T(string source) => UiText.Translate(source);

    private sealed record ShortcutEntry(string Category, string Shortcut, string Description)
    {
        public string AccessibleName => $"{Category}. {Shortcut}. {Description}";
    }
}
