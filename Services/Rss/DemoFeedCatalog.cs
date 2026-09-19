using System.Globalization;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed record DemoFeedDefinition(string Key, string Name, string Url);

/// <summary>
/// Catalogul de feeduri demonstrative este separat de feedurile utilizatorului.
/// Feedul local funcționează fără internet; exemplele online sunt opționale și sunt
/// adăugate numai după ce adresa răspunde cu articole RSS/Atom.
/// </summary>
public static class DemoFeedCatalog
{
    public const string LocalUrl = "demo://orizont-local";

    private static readonly IReadOnlyDictionary<string, DemoFeedDefinition> OnlineFeeds =
        new Dictionary<string, DemoFeedDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["ro-RO"] = new("ro-RO", "HotNews.ro — ultimele știri", "https://hotnews.ro/feed"),
            ["en-US"] = new("en-US", "BBC News — World", "https://feeds.bbci.co.uk/news/rss.xml"),
            ["es-ES"] = new("es-ES", "RTVE Noticias", "https://www.rtve.es/rss/temas_noticias.xml"),
            ["fr-FR"] = new("fr-FR", "Le Monde — À la une", "https://www.lemonde.fr/rss/une.xml"),
            ["de-DE"] = new("de-DE", "tagesschau.de — Startseite", "https://www.tagesschau.de/index~rss2.xml"),
            ["pt-BR"] = new("pt-BR", "RTP Notícias — Últimas", "https://www.rtp.pt/noticias/rss"),
            ["hu-HU"] = new("hu-HU", "Telex — hírek", "https://telex.hu/rss"),
            ["it-IT"] = new("it-IT", "ANSA — Ultime notizie", "https://www.ansa.it/sito/notizie/topnews/topnews_rss.xml")
        };

    private static readonly IReadOnlyDictionary<string, string> FolderNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ro-RO"] = "Exemple",
            ["en-US"] = "Examples",
            ["es-ES"] = "Ejemplos",
            ["fr-FR"] = "Exemples",
            ["de-DE"] = "Beispiele",
            ["pt-BR"] = "Exemplos",
            ["hu-HU"] = "Példák",
            ["it-IT"] = "Esempi"
        };

    private static readonly IReadOnlyDictionary<string, string> LocalNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ro-RO"] = "Feed demonstrativ local",
            ["en-US"] = "Local demonstration feed",
            ["es-ES"] = "Feed de demostración local",
            ["fr-FR"] = "Flux de démonstration local",
            ["de-DE"] = "Lokaler Demo-Feed",
            ["pt-BR"] = "Feed de demonstração local",
            ["hu-HU"] = "Helyi bemutató hírcsatorna",
            ["it-IT"] = "Feed dimostrativo locale"
        };

    private static readonly IReadOnlyDictionary<string, (string Title, string Content)[]> LocalArticles =
        new Dictionary<string, (string, string)[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["ro-RO"] =
            [
                ("Bun venit în Orizont RSS", "Acesta este un articol demonstrativ local. Navighează cu săgețile, apasă Enter pentru cititor și Shift+F10 pentru meniul contextual."),
                ("Accesibilitatea începe cu tastatura", "Exemplul arată cum sunt anunțate starea articolului, sursa și data publicării de către cititoarele de ecran."),
                ("Organizează-ți știrile", "Folosește folderele, etichetele, favoritele și lista Mai târziu pentru a păstra controlul asupra fluxurilor.")
            ],
            ["en-US"] =
            [
                ("Welcome to Orizont RSS", "This is a local demonstration article. Use the arrow keys, press Enter for the reader, and Shift+F10 for the context menu."),
                ("Accessibility starts with the keyboard", "This example shows how article state, source, and publication date are announced by screen readers."),
                ("Organize your news", "Use folders, tags, favorites, and Read later to keep control of your feeds.")
            ],
            ["es-ES"] =
            [
                ("Bienvenido a Orizont RSS", "Este es un artículo de demostración local. Usa las flechas, pulsa Enter para el lector y Shift+F10 para el menú contextual."),
                ("La accesibilidad empieza con el teclado", "Este ejemplo muestra cómo los lectores de pantalla anuncian el estado, la fuente y la fecha del artículo."),
                ("Organiza tus noticias", "Usa carpetas, etiquetas, favoritos y Leer más tarde para mantener el control de tus fuentes.")
            ],
            ["fr-FR"] =
            [
                ("Bienvenue dans Orizont RSS", "Ceci est un article de démonstration local. Utilisez les flèches, appuyez sur Entrée pour le lecteur et Maj+F10 pour le menu contextuel."),
                ("L'accessibilité commence au clavier", "Cet exemple montre comment les lecteurs d'écran annoncent l'état, la source et la date de publication."),
                ("Organisez vos actualités", "Utilisez les dossiers, les étiquettes, les favoris et la liste À lire plus tard pour garder le contrôle.")
            ],
            ["de-DE"] =
            [
                ("Willkommen bei Orizont RSS", "Dies ist ein lokaler Demoartikel. Verwenden Sie die Pfeiltasten, Enter für den Reader und Umschalt+F10 für das Kontextmenü."),
                ("Barrierefreiheit beginnt mit der Tastatur", "Dieses Beispiel zeigt, wie Screenreader Status, Quelle und Veröffentlichungsdatum ankündigen."),
                ("Ordnen Sie Ihre Nachrichten", "Verwenden Sie Ordner, Tags, Favoriten und Später lesen, um Ihre Feeds zu verwalten.")
            ],
            ["pt-BR"] =
            [
                ("Bem-vindo ao Orizont RSS", "Este é um artigo de demonstração local. Use as setas, pressione Enter para o leitor e Shift+F10 para o menu de contexto."),
                ("A acessibilidade começa pelo teclado", "Este exemplo mostra como leitores de tela anunciam o estado, a fonte e a data de publicação."),
                ("Organize suas notícias", "Use pastas, etiquetas, favoritos e Ler mais tarde para manter o controle dos seus feeds.")
            ],
            ["hu-HU"] =
            [
                ("Üdvözli az Orizont RSS", "Ez egy helyi bemutatócikk. Használja a nyílbillentyűket, az Entert az olvasóhoz és a Shift+F10-et a helyi menühöz."),
                ("A hozzáférhetőség a billentyűzettel kezdődik", "A példa megmutatja, hogyan jelzik a képernyőolvasók a cikk állapotát, forrását és dátumát."),
                ("Rendezze híreit", "Mappákkal, címkékkel, kedvencekkel és a későbbi olvasás listájával kezelheti hírcsatornáit.")
            ],
            ["it-IT"] =
            [
                ("Benvenuto in Orizont RSS", "Questo è un articolo dimostrativo locale. Usa le frecce, premi Invio per il lettore e Maiusc+F10 per il menu contestuale."),
                ("L'accessibilità comincia dalla tastiera", "L'esempio mostra come gli screen reader annunciano stato, fonte e data di pubblicazione."),
                ("Organizza le tue notizie", "Usa cartelle, etichette, preferiti e Leggi più tardi per gestire i tuoi feed.")
            ]
        };

    public static string CurrentLanguageCode() => UiCulture.Resolve(CultureInfo.CurrentUICulture.Name);
    public static string CurrentLanguageName() => UiCulture.SupportedLanguages.First(language => string.Equals(language.Code, CurrentLanguageCode(), StringComparison.OrdinalIgnoreCase)).NativeName;
    public static string DemoFolder(string? languageCode = null) => FolderNames.TryGetValue(languageCode ?? CurrentLanguageCode(), out var folder) ? folder : "Examples";
    public static DemoFeedDefinition? OnlineForCurrentLanguage() => OnlineForLanguage(CurrentLanguageCode());
    public static DemoFeedDefinition? OnlineForLanguage(string? languageCode) => languageCode is not null && OnlineFeeds.TryGetValue(UiCulture.Resolve(languageCode), out var definition) ? definition : null;
    public static bool IsLocal(Feed feed) => string.Equals(feed.Url, LocalUrl, StringComparison.OrdinalIgnoreCase);
    public static bool IsDemo(Feed feed) => feed.IsDemo || IsLocal(feed);

    public static Feed CreateLocalFeed(string? languageCode = null)
    {
        var code = UiCulture.Resolve(languageCode ?? CurrentLanguageCode());
        var name = LocalNames.TryGetValue(code, out var localizedName) ? localizedName : LocalNames["en-US"];
        var articles = LocalArticles.TryGetValue(code, out var localizedArticles) ? localizedArticles : LocalArticles["en-US"];
        var published = DateTimeOffset.Now;
        return new Feed
        {
            Name = name,
            Url = LocalUrl,
            Folder = DemoFolder(code),
            IsDemo = true,
            LastSuccessfulUpdate = published,
            LastArticleReceivedOn = published,
            Articles = articles.Select((article, index) => new Article
            {
                Id = $"{LocalUrl}/{code}/{index + 1}",
                Title = article.Title,
                Content = article.Content,
                FullContent = article.Content,
                Link = $"{LocalUrl}/{code}/{index + 1}",
                Published = published.AddMinutes(-index),
                IsRead = index > 0,
                IsFavorite = index == 1,
                ReadLater = index == 2
            }).ToList()
        };
    }
}
