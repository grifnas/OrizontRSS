using System.Globalization;
using System.Text.Json.Serialization;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed class Feed
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Folder { get; set; } = "Neorganizate";
    public int ConsecutiveFailures { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset AddedOn { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? LastSuccessfulUpdate { get; set; }
    public DateTimeOffset? LastArticleReceivedOn { get; set; }
    public List<Article> Articles { get; set; } = [];
    public bool HasThreeMonthSilence => DateTimeOffset.Now - (LastArticleReceivedOn ?? LastSuccessfulUpdate ?? AddedOn) >= TimeSpan.FromDays(90);
    public bool NeedsAttention => ConsecutiveFailures >= 3 || HasThreeMonthSilence;
    public string AttentionReason => ConsecutiveFailures >= 3 ? UiText.Translate("după 3 erori consecutive") : UiText.Translate("nu a primit articole de peste 3 luni");
    public string DisplayName => UiText.Format("{0}{1}, folder: {2}, {3} articole", NeedsAttention ? UiText.Format("Necesită atenție: {0}. ", AttentionReason) : string.Empty, Name, Folder, Articles.Count);
    public string VisualDetails => UiText.Format("Folder: {0} · {1} articole", Folder, Articles.Count);
    public string VisualWarning => NeedsAttention ? UiText.Format("Necesită atenție: {0}", AttentionReason) : string.Empty;
}

public sealed class Article
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? FullContent { get; set; }
    public string Link { get; set; } = string.Empty;
    public DateTimeOffset Published { get; set; } = DateTimeOffset.Now;
    public bool IsRead { get; set; }
    public bool IsFavorite { get; set; }
    public bool ReadLater { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<AiNote> AiNotes { get; set; } = [];
    [JsonIgnore] public string SourceName { get; set; } = string.Empty;
    [JsonIgnore] public bool IncludeSourceInDisplay { get; set; }
    private string SourceAnnouncement => IncludeSourceInDisplay && !string.IsNullOrWhiteSpace(SourceName)
        ? UiText.Format("Sursa: {0}. ", SourceName)
        : string.Empty;
    public string DisplayName => UiText.Format("{0}. {1}{2}{3}{4}. {5}{6}.",
        IsRead ? UiText.Translate("Citit") : UiText.Translate("Necitit"),
        IsFavorite ? UiText.Translate("Favorit. ") : string.Empty,
        ReadLater ? UiText.Translate("De citit mai târziu. ") : string.Empty,
        Tags?.Count > 0 ? UiText.Format("Etichete: {0}. ", string.Join(", ", Tags)) : string.Empty,
        Title,
        SourceAnnouncement,
        Published.ToString("dd MMMM yyyy, HH:mm", CultureInfo.CurrentCulture));
    public string VisualDetails => $"{(IncludeSourceInDisplay && !string.IsNullOrWhiteSpace(SourceName) ? UiText.Format("Sursa: {0} · ", SourceName) : string.Empty)}{Published.ToString("dd MMM yyyy, HH:mm", CultureInfo.CurrentCulture)}{(Tags?.Count > 0 ? $" · {string.Join(", ", Tags)}" : string.Empty)}";
    public string VisualState => $"{(IsRead ? UiText.Translate("Citit") : UiText.Translate("Necitit"))}{(IsFavorite ? UiText.Translate(" · Favorit") : string.Empty)}{(ReadLater ? UiText.Translate(" · Mai târziu") : string.Empty)}";
}

public sealed class AiNote
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}
