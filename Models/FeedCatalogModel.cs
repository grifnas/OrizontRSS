using System.Text.Json.Serialization;

namespace CititorRSS.Jaws.Models;

public sealed class CatalogFeedItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "ro";

    public bool IsLiveSource { get; set; }

    public string DisplayName =>
        $"{Name}. Categorie: {Category}. {Description}{(IsLiveSource ? " (Sursă web live)" : " (Catalog local)")}";

    public override string ToString() => DisplayName;
}

public sealed class CatalogCategory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("feeds")]
    public List<CatalogFeedItem> Feeds { get; set; } = new();
}

public sealed class CatalogRoot
{
    [JsonPropertyName("categories")]
    public List<CatalogCategory> Categories { get; set; } = new();
}
