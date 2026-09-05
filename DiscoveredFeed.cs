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
    public string Status => IsVerified ? UiText.Format("Verificat: {0} articole disponibile.", ArticleCount) : UiText.Translate("Neverificat. Verifică înainte de adăugare.");
    public string DisplayName => UiText.Format("{0}. {1}", Name, Status);
}
