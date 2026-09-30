namespace CititorRSS.Jaws;

public sealed class AiProviderService(GeminiConnection? gemini = null, OpenAiConnection? openAi = null, MistralConnection? mistral = null, DeepSeekConnection? deepSeek = null)
{
    private readonly GeminiConnection _gemini = gemini ?? new GeminiConnection();
    private readonly OpenAiConnection _openAi = openAi ?? new OpenAiConnection();
    private readonly MistralConnection _mistral = mistral ?? new MistralConnection();
    private readonly DeepSeekConnection _deepSeek = deepSeek ?? new DeepSeekConnection();

    public Task<string> GenerateAsync(string provider, string key, string model, string instructions, string prompt) =>
        AiProviderIds.Normalize(provider) switch
        {
            AiProviderIds.OpenAI => _openAi.GenerateAsync(key, model, instructions, prompt),
            AiProviderIds.Mistral => _mistral.GenerateAsync(key, model, instructions, prompt),
            AiProviderIds.DeepSeek => _deepSeek.GenerateAsync(key, model, instructions, prompt),
            _ => _gemini.GenerateAsync(key, instructions, prompt)
        };
}
