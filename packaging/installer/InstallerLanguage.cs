using System.Globalization;

namespace OrizontSetup;

public sealed record InstallerTexts(
    string Code,
    string DisplayName,
    string LanguageDialogTitle,
    string LanguageDialogPrompt,
    string ContinueButton,
    string CancelButton,
    string InstallTitle,
    string InstallIntro,
    string FolderHeader,
    string BrowseButton,
    string DesktopShortcut,
    string DataNotice,
    string InstallButton,
    string Downloading,
    string Verifying,
    string Copying,
    string CreatingShortcuts,
    string Completed,
    string StartButton,
    string CloseButton,
    string UninstallTitle,
    string UninstallIntro,
    string UninstallDataNotice,
    string UninstallButton,
    string ConfirmTitle,
    string ConfirmMessage,
    string MissingFolder,
    string RemovingShortcuts,
    string FinishingUninstall,
    string FailedPrefix,
    string FolderRequired,
    string HashMismatch,
    string MissingInstaller);

public static class InstallerLanguages
{
    public static IReadOnlyList<InstallerTexts> All { get; } = new[]
    {
        new InstallerTexts(
            "ro-RO", "Română", "Limba instalatorului", "Alege limba instalatorului:", "Continuă", "Anulează",
            "Instalare Orizont RSS", "Instalator accesibil pentru Orizont RSS. Aplicația va fi instalată pentru utilizatorul curent.",
            "Folderul instalării", "Răsfoiește...", "Creează și o pictogramă pe desktop",
            "Feedurile, articolele și setările existente nu sunt șterse; ele sunt păstrate în profilul Windows al utilizatorului.",
            "Instalează", "Se descarcă pachetul Orizont RSS...", "Se verifică integritatea pachetului...", "Se copiază fișierele aplicației...", "Se creează scurtăturile...", "Instalarea s-a finalizat cu succes.", "Pornește Orizont RSS", "Închide",
            "Dezinstalare Orizont RSS", "Aplicația și scurtăturile Orizont RSS vor fi eliminate.", "Feedurile, articolele și setările din profilul Windows nu sunt șterse.", "Dezinstalează",
            "Confirmă dezinstalarea", "Sigur dorești să dezinstalezi Orizont RSS? Datele utilizatorului nu vor fi șterse.", "Folderul instalării nu mai există.", "Se elimină scurtăturile...", "Se finalizează dezinstalarea...", "Operația nu a reușit:", "Alege un folder pentru instalare.", "Hash-ul pachetului descărcat nu corespunde Release-ului oficial.", "Nu s-a putut determina instalatorul."),
        new InstallerTexts(
            "en-US", "English", "Installer language", "Choose the installer language:", "Continue", "Cancel",
            "Install Orizont RSS", "Accessible installer for Orizont RSS. The application will be installed for the current user.",
            "Installation folder", "Browse...", "Create a desktop shortcut",
            "Existing feeds, articles and settings are not deleted; they remain in the Windows user profile.",
            "Install", "Downloading the Orizont RSS package...", "Checking package integrity...", "Copying application files...", "Creating shortcuts...", "Installation completed successfully.", "Start Orizont RSS", "Close",
            "Uninstall Orizont RSS", "The Orizont RSS application and shortcuts will be removed.", "Feeds, articles and settings in the Windows user profile will not be deleted.", "Uninstall",
            "Confirm uninstall", "Are you sure you want to uninstall Orizont RSS? User data will not be deleted.", "The installation folder no longer exists.", "Removing shortcuts...", "Finishing uninstall...", "The operation failed:", "Choose an installation folder.", "The downloaded package hash does not match the official Release.", "The installer path could not be determined."),
        new InstallerTexts(
            "es-ES", "Español", "Idioma del instalador", "Elige el idioma del instalador:", "Continuar", "Cancelar",
            "Instalar Orizont RSS", "Instalador accesible de Orizont RSS. La aplicación se instalará para el usuario actual.",
            "Carpeta de instalación", "Examinar...", "Crear un acceso directo en el escritorio",
            "Los feeds, artículos y configuraciones existentes no se eliminarán; permanecerán en el perfil de usuario de Windows.",
            "Instalar", "Descargando el paquete de Orizont RSS...", "Comprobando la integridad del paquete...", "Copiando los archivos de la aplicación...", "Creando accesos directos...", "La instalación se completó correctamente.", "Iniciar Orizont RSS", "Cerrar",
            "Desinstalar Orizont RSS", "Se eliminarán la aplicación y los accesos directos de Orizont RSS.", "Los feeds, artículos y configuraciones del perfil de Windows no se eliminarán.", "Desinstalar",
            "Confirmar desinstalación", "¿Seguro que quieres desinstalar Orizont RSS? No se eliminarán los datos del usuario.", "La carpeta de instalación ya no existe.", "Eliminando accesos directos...", "Finalizando la desinstalación...", "La operación no se realizó:", "Elige una carpeta de instalación.", "El hash del paquete descargado no coincide con la versión oficial.", "No se pudo determinar el instalador."),
        new InstallerTexts(
            "fr-FR", "Français", "Langue de l’installation", "Choisissez la langue de l’installation :", "Continuer", "Annuler",
            "Installer Orizont RSS", "Programme d’installation accessible d’Orizont RSS. L’application sera installée pour l’utilisateur actuel.",
            "Dossier d’installation", "Parcourir...", "Créer un raccourci sur le bureau",
            "Les flux, articles et paramètres existants ne seront pas supprimés ; ils restent dans le profil utilisateur Windows.",
            "Installer", "Téléchargement du paquet Orizont RSS...", "Vérification de l’intégrité du paquet...", "Copie des fichiers de l’application...", "Création des raccourcis...", "L’installation est terminée.", "Démarrer Orizont RSS", "Fermer",
            "Désinstaller Orizont RSS", "L’application et les raccourcis Orizont RSS seront supprimés.", "Les flux, articles et paramètres du profil Windows ne seront pas supprimés.", "Désinstaller",
            "Confirmer la désinstallation", "Voulez-vous vraiment désinstaller Orizont RSS ? Les données utilisateur ne seront pas supprimées.", "Le dossier d’installation n’existe plus.", "Suppression des raccourcis...", "Finalisation de la désinstallation...", "L’opération a échoué :", "Choisissez un dossier d’installation.", "Le hachage du paquet téléchargé ne correspond pas à la version officielle.", "Le chemin de l’installateur est introuvable."),
        new InstallerTexts(
            "de-DE", "Deutsch", "Sprache des Installationsprogramms", "Wählen Sie die Sprache des Installationsprogramms:", "Weiter", "Abbrechen",
            "Orizont RSS installieren", "Barrierefreies Installationsprogramm für Orizont RSS. Die Anwendung wird für den aktuellen Benutzer installiert.",
            "Installationsordner", "Durchsuchen...", "Desktop-Verknüpfung erstellen",
            "Vorhandene Feeds, Artikel und Einstellungen werden nicht gelöscht; sie bleiben im Windows-Benutzerprofil erhalten.",
            "Installieren", "Orizont-RSS-Paket wird heruntergeladen...", "Paketintegrität wird geprüft...", "Anwendungsdateien werden kopiert...", "Verknüpfungen werden erstellt...", "Installation erfolgreich abgeschlossen.", "Orizont RSS starten", "Schließen",
            "Orizont RSS deinstallieren", "Die Orizont-RSS-Anwendung und Verknüpfungen werden entfernt.", "Feeds, Artikel und Einstellungen im Windows-Benutzerprofil werden nicht gelöscht.", "Deinstallieren",
            "Deinstallation bestätigen", "Möchten Sie Orizont RSS wirklich deinstallieren? Benutzerdaten werden nicht gelöscht.", "Der Installationsordner ist nicht mehr vorhanden.", "Verknüpfungen werden entfernt...", "Deinstallation wird abgeschlossen...", "Der Vorgang ist fehlgeschlagen:", "Wählen Sie einen Installationsordner.", "Der Hash des heruntergeladenen Pakets stimmt nicht mit der offiziellen Version überein.", "Der Pfad des Installationsprogramms konnte nicht ermittelt werden."),
        new InstallerTexts(
            "pt-BR", "Português", "Idioma do instalador", "Escolha o idioma do instalador:", "Continuar", "Cancelar",
            "Instalar o Orizont RSS", "Instalador acessível do Orizont RSS. O aplicativo será instalado para o usuário atual.",
            "Pasta de instalação", "Procurar...", "Criar um atalho na área de trabalho",
            "Feeds, artigos e configurações existentes não serão excluídos; eles permanecem no perfil do usuário do Windows.",
            "Instalar", "Baixando o pacote do Orizont RSS...", "Verificando a integridade do pacote...", "Copiando os arquivos do aplicativo...", "Criando atalhos...", "A instalação foi concluída com sucesso.", "Iniciar o Orizont RSS", "Fechar",
            "Desinstalar o Orizont RSS", "O aplicativo e os atalhos do Orizont RSS serão removidos.", "Feeds, artigos e configurações do perfil do Windows não serão excluídos.", "Desinstalar",
            "Confirmar desinstalação", "Tem certeza de que deseja desinstalar o Orizont RSS? Os dados do usuário não serão excluídos.", "A pasta de instalação não existe mais.", "Removendo atalhos...", "Finalizando a desinstalação...", "A operação falhou:", "Escolha uma pasta de instalação.", "O hash do pacote baixado não corresponde à versão oficial.", "Não foi possível determinar o instalador.")
    };

    public static InstallerTexts FromCode(string code) => All.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) ?? All[0];

    public static string ResolveCode(string? cultureName)
    {
        var name = cultureName ?? CultureInfo.CurrentUICulture.Name;
        if (name.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return "en-US";
        if (name.StartsWith("es", StringComparison.OrdinalIgnoreCase)) return "es-ES";
        if (name.StartsWith("fr", StringComparison.OrdinalIgnoreCase)) return "fr-FR";
        if (name.StartsWith("de", StringComparison.OrdinalIgnoreCase)) return "de-DE";
        if (name.StartsWith("pt", StringComparison.OrdinalIgnoreCase)) return "pt-BR";
        return "ro-RO";
    }
}
