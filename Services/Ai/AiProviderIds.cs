namespace CititorRSS.Jaws;

public static class AiProviderIds
{
    public const string Gemini = "Gemini";
    public const string OpenAI = "OpenAI";
    public const string Mistral = "Mistral";
    public const string DeepSeek = "DeepSeek";

    public static string DisplayName(string? value) => Normalize(value) switch
    {
        OpenAI => "OpenAI",
        Mistral => "Mistral",
        DeepSeek => "DeepSeek",
        _ => "Gemini"
    };

    public static string Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "openai" => OpenAI,
        "mistral" => Mistral,
        "deepseek" => DeepSeek,
        _ => Gemini
    };

    public static IReadOnlyList<string> All { get; } = [Gemini, OpenAI, Mistral, DeepSeek];
}
