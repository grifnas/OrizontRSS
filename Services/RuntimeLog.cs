using System.IO;
using System.Net.Http;
using System.Text;

namespace CititorRSS.Jaws.Services;

/// <summary>
/// Jurnal tehnic de funcționare: pornire, oprire, actualizări de feeduri și erori.
/// Nu conține titluri sau text de articole, adrese de feeduri, chei ori sesiuni.
/// Este inactiv până la <see cref="Start"/>, deci testele nu scriu în profilul utilizatorului.
/// </summary>
internal static class RuntimeLog
{
    internal const string FileName = "jurnal.txt";
    internal const string PreviousFileName = "jurnal-anterior.txt";
    internal const long MaxBytes = 512 * 1024;

    private static readonly object Gate = new();
    private static string? _folder;

    internal static void Start(string folder)
    {
        lock (Gate) _folder = folder;
    }

    internal static void Stop()
    {
        lock (Gate) _folder = null;
    }

    internal static void Write(string message)
    {
        lock (Gate)
        {
            if (_folder is null) return;
            try
            {
                Directory.CreateDirectory(_folder);
                var path = Path.Combine(_folder, FileName);
                var file = new FileInfo(path);
                // La depășirea limitei, jurnalul curent devine cel anterior; se păstrează cel mult două fișiere.
                if (file.Exists && file.Length >= MaxBytes) File.Move(path, Path.Combine(_folder, PreviousFileName), overwrite: true);
                var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}  {message.ReplaceLineEndings(" ")}{Environment.NewLine}";
                File.AppendAllText(path, line, Encoding.UTF8);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Jurnalul este opțional: o eroare de scriere nu trebuie să oprească aplicația.
            }
        }
    }

    /// <summary>Tipul erorii și, pentru HTTP, codul de stare; mesajul este omis, fiindcă poate conține adrese.</summary>
    internal static string Summarize(Exception exception)
    {
        var status = (exception as HttpRequestException)?.StatusCode;
        return status is null ? exception.GetType().Name : $"{exception.GetType().Name}, HTTP {(int)status}";
    }
}
