using System.Windows;

namespace CititorRSS.Jaws.Services.Content;

internal sealed record ArticleTranslationResult(
    string OriginalText,
    string TranslatedText,
    string Title,
    string Link,
    string Provider,
    string Language);

// Side effects used by article commands. Tests substitute these without opening
// windows, contacting services or touching the user's profile.
internal sealed class ArticleCommandPorts
{
    public required Func<string, Task<string>> LoadReadableAsync { get; init; }
    public required Func<string, string, Task<string?>> LoadNewsBlurTextAsync { get; init; }
    public required Func<string?, string> Unprotect { get; init; }
    public required Func<string, string, string, Task<string>> TranslateDeepLAsync { get; init; }
    public required Func<string, string, string, CancellationToken, Task<string>> TranslateGoogleAsync { get; init; }
    public required Func<Task> SaveAsync { get; init; }
    public required Action<Article, string, Func<Task<string>>> ShowReader { get; init; }
    public required Action<ArticleTranslationResult> ShowTranslation { get; init; }
    public required Func<bool> ConfirmGoogle { get; init; }
    public required Action<string, string, MessageBoxImage> ShowError { get; init; }
}
