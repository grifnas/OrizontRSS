namespace CititorRSS.Jaws;

public sealed class AppUpdateInfo
{
    public bool HasUpdate { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ReleasePageUrl { get; set; } = string.Empty;
}
