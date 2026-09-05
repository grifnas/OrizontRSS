using System.IO;
using System.Text.Json;

namespace CititorRSS.Jaws;

public sealed class FeedStore
{
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CititorRSS-JAWS", "feeds.json");
    private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CititorRSS-JAWS", "settings.json");
    public static string LoadUiLanguageForStartup()
    {
        if (!File.Exists(SettingsPath)) return Localization.UiCulture.Automatic;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(SettingsPath));
            if (document.RootElement.TryGetProperty(nameof(AppSettings.UiLanguage), out var language) && language.ValueKind == JsonValueKind.String)
                return Localization.UiCulture.NormalizeSelection(language.GetString());
            // Setările create înainte de 1.4 rămân în română, indiferent de limba Windows.
            return "ro-RO";
        }
        catch
        {
            return "ro-RO";
        }
    }
    public async Task<List<Feed>> LoadAsync()
    {
        if (!File.Exists(FilePath)) return [];
        await using var stream = File.OpenRead(FilePath);
        var feeds = await JsonSerializer.DeserializeAsync<List<Feed>>(stream) ?? [];
        Normalize(feeds);
        return feeds;
    }
    public async Task SaveAsync(List<Feed> feeds)
    {
        await SaveJsonAsync(FilePath, feeds);
    }
    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (!File.Exists(SettingsPath)) return new AppSettings { UiLanguage = Localization.UiCulture.Automatic };
        await using var stream = File.OpenRead(SettingsPath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? new AppSettings();
        settings.UiLanguage ??= "ro-RO";
        return settings;
    }
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await SaveJsonAsync(SettingsPath, settings);
    }

    private async Task SaveJsonAsync<T>(string destination, T value)
    {
        // Snapshotul este creat sincron pe firul interfeței, înainte ca alte comenzi să poată modifica listele.
        var snapshot = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        await _saveGate.WaitAsync();
        var folder = Path.GetDirectoryName(destination)!;
        try
        {
            Directory.CreateDirectory(folder);
            var temporary = Path.Combine(folder, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");
            try
            {
                await File.WriteAllBytesAsync(temporary, snapshot);
                File.Move(temporary, destination, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private static void Normalize(List<Feed> feeds)
    {
        feeds.RemoveAll(feed => feed is null);
        foreach (var feed in feeds)
        {
            feed.Name ??= string.Empty;
            feed.Url ??= string.Empty;
            feed.Folder = string.IsNullOrWhiteSpace(feed.Folder) ? "Neorganizate" : feed.Folder;
            feed.Articles ??= [];
            feed.Articles.RemoveAll(article => article is null);
            foreach (var article in feed.Articles)
            {
                article.Id ??= string.Empty;
                article.Title ??= string.Empty;
                article.Content ??= string.Empty;
                article.Link ??= string.Empty;
                article.Tags ??= [];
                article.AiNotes ??= [];
            }
        }
    }
}
