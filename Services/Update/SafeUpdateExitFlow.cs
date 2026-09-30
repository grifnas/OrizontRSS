namespace CititorRSS.Jaws.Services.Update;

internal static class SafeUpdateExitFlow
{
    public static async Task<bool> SaveThenStartAndCloseAsync(
        Func<Task> saveAsync,
        Func<bool> startInstaller,
        Action closeApplication)
    {
        await saveAsync();
        if (!startInstaller()) return false;
        closeApplication();
        return true;
    }
}
