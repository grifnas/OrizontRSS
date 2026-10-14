using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed class DiscoveredFeed
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SourceSite { get; set; } = string.Empty;
    public bool RequiresVerification { get; set; }
    public bool IsVerified { get; set; }
    public int ArticleCount { get; set; }
    /// <summary>Data celui mai recent articol din flux, citită la verificare.</summary>
    public DateTimeOffset? LatestArticle { get; set; }
    /// <summary>Fluxul conține comentariile cititorilor, nu articolele site-ului.</summary>
    public bool IsComments { get; set; }
    public string Status => IsVerified
        ? UiText.Format("Verificat: {0} articole disponibile.", ArticleCount) +
          (LatestArticle is { } latest ? UiText.Format(" Ultimul articol: {0}.", latest.ToLocalTime().ToString("d MMMM yyyy, HH:mm", System.Globalization.CultureInfo.CurrentCulture)) : string.Empty) +
          (IsComments ? UiText.Translate(" Flux de comentarii.") : string.Empty)
        : UiText.Translate("Neverificat. Verifică înainte de adăugare.");
    public string DisplayName => UiText.Format("{0}. {1}", Name, Status);
}
