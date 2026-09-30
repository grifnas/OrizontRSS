namespace CititorRSS.Jaws;

public sealed class AppUpdateInfo
{
    public UpdateCheckStatus Status { get; set; } = UpdateCheckStatus.Error;
    public bool HasUpdate => Status == UpdateCheckStatus.UpdateAvailable;
    public string LatestVersion { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ChecksumDownloadUrl { get; set; } = string.Empty;
    public string ReleasePageUrl { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public enum UpdateCheckStatus
{
    Error,
    UpToDate,
    UpdateAvailable
}
