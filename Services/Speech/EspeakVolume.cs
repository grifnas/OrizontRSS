namespace CititorRSS.Jaws;

/// <summary>
/// eSpeak NG acceptă volume de la 0 la 200, unde 100 este nivelul „normal”, mai încet decât vocile SAPI.
/// Setarea aplicației (0–100) acoperă întreaga gamă; motorul își limitează singur vârfurile.
/// </summary>
public static class EspeakVolume
{
    public static int FromSetting(int volumePercent) => Math.Clamp(volumePercent, 0, 100) * 2;
}
