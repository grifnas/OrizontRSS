namespace CititorRSS.Jaws;

public sealed class FeedCatalog
{
    private static readonly (string Name, string Url, string Folder, string Keywords)[] Entries =
    [
        ("HotNews.ro — General", "https://hotnews.ro/feed", "Actualitate", "hotnews actualitate stiri romania politica economie"),
        ("RoPress — Actualitate", "https://www.ropress.ro/rss/category/actualitate", "Actualitate", "actualitate stiri romania politica"),
        ("RoPress — Economie", "https://www.ropress.ro/rss/category/economie", "Economie", "economie finante afaceri bani"),
        ("RoPress — Știință și tehnologie", "https://www.ropress.ro/rss/category/stiinta-tehnologie", "Tehnologie", "tehnologie stiinta it ai inteligenta artificiala digital"),
        ("RoPress — Artă și cultură", "https://www.ropress.ro/rss/category/arta-cultura", "Cultură", "cultura arta literatura carti film"),
        ("BBC News — Technology", "https://feeds.bbci.co.uk/news/technology/rss.xml", "Tehnologie", "technology tehnologie it ai inteligenta artificiala digital"),
        ("BBC News — Business", "https://feeds.bbci.co.uk/news/business/rss.xml", "Economie", "business economie finante afaceri"),
        ("BBC News — Science and Environment", "https://feeds.bbci.co.uk/news/science_and_environment/rss.xml", "Știință", "science stiinta mediu clima sanatate"),
        ("Hacker News — Front Page", "https://hnrss.org/frontpage", "Tehnologie", "tehnologie programare software startup ai"),
        ("The Verge — Technology", "https://www.theverge.com/rss/index.xml", "Tehnologie", "tehnologie gadget ai jocuri"),
        ("Financiarul — Economie", "https://financiarul.ro/stiri/economie/feed", "Economie", "economie finante afaceri bani romania"),
        ("Financiarul — Investiții", "https://financiarul.ro/stiri/investitii/feed", "Economie", "investitii bursa economie finante")
    ];
    public List<DiscoveredFeed> Search(string query)
    {
        var words = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return [];
        return Entries.Where(entry =>
        {
            var searchable = Normalize($"{entry.Name} {entry.Folder} {entry.Keywords}");
            return words.All(word => searchable.Contains(word, StringComparison.Ordinal));
        }).Select(entry => new DiscoveredFeed { Name = entry.Name, Url = entry.Url, SourceSite = entry.Folder }).ToList();
    }
    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(System.Text.NormalizationForm.FormD);
        return new string(decomposed.Where(character => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
    }
}
