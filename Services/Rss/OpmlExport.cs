using System.Xml.Linq;

namespace CititorRSS.Jaws;

/// <summary>
/// Corpul OPML exportat: folderele numite devin grupuri, iar feedurile neorganizate stau la rădăcină,
/// ca în celelalte cititoare RSS. Importul Orizont pune feedurile de la rădăcină în „Neorganizate”.
/// </summary>
public static class OpmlExport
{
    public static XElement CreateBody(IEnumerable<Feed> feeds)
    {
        var all = feeds.ToList();
        var folders = all.Where(feed => NewsBlurFolderMapping.IsNamedFolder(feed.Folder))
            .GroupBy(feed => feed.Folder.Trim(), StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.CurrentCulture)
            .Select(group => new XElement("outline",
                new XAttribute("text", group.Key),
                new XAttribute("title", group.Key),
                group.OrderBy(feed => feed.Name).Select(FeedOutline)));
        var rootFeeds = all.Where(feed => !NewsBlurFolderMapping.IsNamedFolder(feed.Folder))
            .OrderBy(feed => feed.Name)
            .Select(FeedOutline);
        return new XElement("body", folders, rootFeeds);
    }

    private static XElement FeedOutline(Feed feed) => new("outline",
        new XAttribute("text", feed.Name),
        new XAttribute("title", feed.Name),
        new XAttribute("type", "rss"),
        new XAttribute("xmlUrl", feed.Url));
}
