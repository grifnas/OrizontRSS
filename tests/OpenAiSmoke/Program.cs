using System.Net;
using System.Text;
using System.Text.Json;
using CititorRSS.Jaws;

var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException($"Failed: {message}");
}

Check(new AppSettings().AiDefaultProvider == AiProviderIds.Gemini, "existing installations keep Gemini as the default");
Check(new AppSettings().DeepSeekModel == "deepseek-flash", "DeepSeek uses a current API model as its initial selection");
Check(AiProviderIds.Normalize("OpenAI") == AiProviderIds.OpenAI, "OpenAI provider id is normalized");
Check(AiProviderIds.Normalize(" mistral ") == AiProviderIds.Mistral, "Mistral provider id is normalized");
Check(AiProviderIds.Normalize(" deepseek ") == AiProviderIds.DeepSeek, "DeepSeek provider id is normalized");
Check(AiProviderIds.Normalize("unknown") == AiProviderIds.Gemini, "unknown provider safely falls back to Gemini");
Check(AiProviderIds.All.Contains(AiProviderIds.DeepSeek), "DeepSeek is available in the provider selector list");

var originalSettings = new AppSettings
{
    AiDefaultProvider = AiProviderIds.OpenAI,
    OpenAiEnabled = true,
    EncryptedOpenAiKey = "encrypted-openai-secret",
    OpenAiModel = "gpt-4.1",
    GeminiEnabled = true,
    EncryptedGeminiKey = "encrypted-gemini-secret"
};
var backup = BackupPolicy.SanitizeSettings(originalSettings);
Check(backup.AiDefaultProvider == AiProviderIds.OpenAI && backup.OpenAiModel == "gpt-4.1", "backup preserves the provider preference and model");
Check(!backup.OpenAiEnabled && backup.EncryptedOpenAiKey is null, "backup excludes OpenAI credentials");
Check(!backup.GeminiEnabled && backup.EncryptedGeminiKey is null, "backup continues to exclude Gemini credentials");

var mistralSettings = new AppSettings
{
    AiDefaultProvider = AiProviderIds.Mistral,
    MistralEnabled = true,
    EncryptedMistralKey = "encrypted-mistral-secret",
    MistralModel = "mistral-large-latest"
};
var mistralBackup = BackupPolicy.SanitizeSettings(mistralSettings);
Check(mistralBackup.AiDefaultProvider == AiProviderIds.Mistral && mistralBackup.MistralModel == "mistral-large-latest", "backup preserves Mistral provider preference and model");
Check(!mistralBackup.MistralEnabled && mistralBackup.EncryptedMistralKey is null, "backup excludes Mistral credentials");

var deepSeekSettings = new AppSettings
{
    AiDefaultProvider = AiProviderIds.DeepSeek,
    DeepSeekEnabled = true,
    EncryptedDeepSeekKey = "encrypted-deepseek-secret",
    DeepSeekModel = "deepseek-v4-pro"
};
var deepSeekBackup = BackupPolicy.SanitizeSettings(deepSeekSettings);
Check(deepSeekBackup.AiDefaultProvider == AiProviderIds.DeepSeek && deepSeekBackup.DeepSeekModel == "deepseek-v4-pro", "backup preserves DeepSeek provider preference and model");
Check(!deepSeekBackup.DeepSeekEnabled && deepSeekBackup.EncryptedDeepSeekKey is null, "backup excludes DeepSeek credentials");

var responseHandler = new StubHandler((request, _) =>
{
    Check(request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/v1/responses", "generation uses the Responses endpoint");
    Check(request.Headers.Authorization?.Scheme == "Bearer" && request.Headers.Authorization.Parameter == "test-secret", "API key is sent as a bearer credential");
    var requestJson = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
    var body = requestJson.RootElement;
    Check(body.GetProperty("model").GetString() == "gpt-4.1", "selected model is sent");
    Check(body.GetProperty("instructions").GetString() == "Be clear", "AI instructions are sent");
    Check(body.GetProperty("input").GetString() == "Article text", "article prompt is sent");
    Check(body.GetProperty("store").ValueKind == JsonValueKind.False, "response storage is disabled");
    return JsonResponse(HttpStatusCode.OK, "{\"output_text\":\"Safe response\"}");
});
using (var http = new HttpClient(responseHandler))
{
    var provider = new AiProviderService(openAi: new OpenAiConnection(http));
    Check(await provider.GenerateAsync(AiProviderIds.OpenAI, "test-secret", "gpt-4.1", "Be clear", "Article text") == "Safe response", "OpenAI response text is returned through provider dispatcher");
}

var modelHandler = new StubHandler((request, _) =>
{
    Check(request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/v1/models", "connection test only lists available models");
    Check(request.Headers.Authorization?.Parameter == "model-secret", "model test uses the supplied key");
    return JsonResponse(HttpStatusCode.OK, """
        {"data":[
          {"id":"gpt-4.1"},
          {"id":"ft:gpt-4.1:org:custom"},
          {"id":"gpt-image-1"},
          {"id":"text-embedding-3-small"},
          {"id":"gpt-realtime"},
          {"id":"dall-e-3"}
        ]}
        """);
});
using (var http = new HttpClient(modelHandler))
{
    var models = await new OpenAiConnection(http).TestAsync("model-secret");
    Check(models.SequenceEqual(new[] { "ft:gpt-4.1:org:custom", "gpt-4.1" }), "test reports text models and filters unrelated model families");
}

var secret = "must-not-leak";
var errorHandler = new StubHandler((_, _) => JsonResponse(HttpStatusCode.Unauthorized, $"{{\"message\":\"invalid key {secret}\"}}"));
using (var http = new HttpClient(errorHandler))
{
    try
    {
        await new OpenAiConnection(http).TestAsync(secret);
        throw new InvalidOperationException("Expected an authentication error.");
    }
    catch (InvalidOperationException exception)
    {
        Check(!exception.Message.Contains(secret, StringComparison.Ordinal), "API key is redacted from error details");
    }
}

var mistralModelHandler = new StubHandler((request, _) =>
{
    Check(request.Method == HttpMethod.Get && request.RequestUri?.AbsoluteUri == "https://api.mistral.ai/v1/models", "Mistral connection test lists available models only");
    Check(request.Headers.Authorization?.Parameter == "mistral-model-secret", "Mistral model test uses the supplied bearer key");
    return JsonResponse(HttpStatusCode.OK, """
        {"data":[
          {"id":"mistral-small-latest","capabilities":{"completion_chat":true}},
          {"id":"mistral-large-latest","capabilities":{"completion_chat":true}},
          {"id":"codestral-latest","capabilities":{"completion_chat":false}},
          {"id":"bad-model","capabilities":[]}
        ]}
        """);
});
using (var http = new HttpClient(mistralModelHandler))
{
    var models = await new MistralConnection(http).TestAsync("mistral-model-secret");
    Check(models.SequenceEqual(new[] { "mistral-large-latest", "mistral-small-latest" }), "Mistral test lists chat-capable models and ignores other or malformed entries");
}

var mistralResponseHandler = new StubHandler((request, _) =>
{
    Check(request.Method == HttpMethod.Post && request.RequestUri?.AbsoluteUri == "https://api.mistral.ai/v1/chat/completions", "Mistral generation uses chat completions endpoint");
    Check(request.Headers.Authorization?.Parameter == "mistral-test-secret", "Mistral generation sends its own bearer key");
    var body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement;
    Check(body.GetProperty("model").GetString() == "mistral-small-latest", "Mistral generation uses the selected model");
    Check(body.GetProperty("messages")[0].GetProperty("role").GetString() == "system" && body.GetProperty("messages")[0].GetProperty("content").GetString() == "Be clear", "Mistral generation uses permanent instructions as system content");
    Check(body.GetProperty("messages")[1].GetProperty("role").GetString() == "user" && body.GetProperty("messages")[1].GetProperty("content").GetString() == "Article text", "Mistral generation sends only the explicit user prompt");
    Check(body.GetProperty("stream").ValueKind == JsonValueKind.False, "Mistral uses a non-streaming request compatible with the existing response window");
    return JsonResponse(HttpStatusCode.OK, """
        {"choices":[{"message":{"role":"assistant","content":"Mistral response"}}]}
        """);
});
using (var http = new HttpClient(mistralResponseHandler))
{
    var provider = new AiProviderService(mistral: new MistralConnection(http));
    Check(await provider.GenerateAsync(AiProviderIds.Mistral, "mistral-test-secret", "mistral-small-latest", "Be clear", "Article text") == "Mistral response", "Mistral response is returned through provider dispatcher");
}

var mistralSecret = "mistral-must-not-leak";
using (var http = new HttpClient(new StubHandler((_, _) => JsonResponse(HttpStatusCode.Unauthorized, $"{{\"message\":\"invalid key {mistralSecret}\"}}"))))
{
    try
    {
        await new MistralConnection(http).TestAsync(mistralSecret);
        throw new InvalidOperationException("Expected a Mistral authentication error.");
    }
    catch (InvalidOperationException exception)
    {
        Check(!exception.Message.Contains(mistralSecret, StringComparison.Ordinal), "Mistral API key is redacted from error details");
    }
}

var deepSeekModelHandler = new StubHandler((request, _) =>
{
    Check(request.Method == HttpMethod.Get && request.RequestUri?.AbsoluteUri == "https://api.deepseek.com/models", "DeepSeek connection test only lists models using the documented endpoint");
    Check(request.Headers.Authorization?.Parameter == "deepseek-model-secret", "DeepSeek model test uses its own bearer key");
    return JsonResponse(HttpStatusCode.OK, """
        {"object":"list","data":[
          {"id":"deepseek-flash","name":"DeepSeek-V4.1-Flash","input_modalities":["text","image"],"output_modalities":["text"]},
          {"id":"deepseek-v4-pro","name":"DeepSeek-V4-Pro","input_modalities":["text"],"output_modalities":["text"]},
          {"id":null},
          {"name":"missing-id"}
        ]}
        """);
});
using (var http = new HttpClient(deepSeekModelHandler))
{
    var models = await new DeepSeekConnection(http).TestAsync("deepseek-model-secret");
    Check(models.SequenceEqual(new[] { "deepseek-flash", "deepseek-v4-pro" }), "DeepSeek connection test returns valid available model IDs and ignores malformed entries");
}

var deepSeekResponseHandler = new StubHandler((request, _) =>
{
    Check(request.Method == HttpMethod.Post && request.RequestUri?.AbsoluteUri == "https://api.deepseek.com/chat/completions", "DeepSeek generation uses the documented chat completions endpoint");
    Check(request.Headers.Authorization?.Parameter == "deepseek-test-secret", "DeepSeek generation sends its own bearer key");
    var body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement;
    Check(body.GetProperty("model").GetString() == "deepseek-flash", "DeepSeek generation uses the selected model");
    Check(body.GetProperty("messages")[0].GetProperty("role").GetString() == "system" && body.GetProperty("messages")[0].GetProperty("content").GetString() == "Be clear", "DeepSeek generation uses permanent instructions as system content");
    Check(body.GetProperty("messages")[1].GetProperty("role").GetString() == "user" && body.GetProperty("messages")[1].GetProperty("content").GetString() == "Article text", "DeepSeek generation sends the explicit user prompt");
    Check(body.GetProperty("stream").ValueKind == JsonValueKind.False, "DeepSeek uses a non-streaming request for the accessible response window");
    return JsonResponse(HttpStatusCode.OK, """
        {"choices":[{"message":{"role":"assistant","content":"DeepSeek response","reasoning_content":"not shown"}}]}
        """);
});
using (var http = new HttpClient(deepSeekResponseHandler))
{
    var provider = new AiProviderService(deepSeek: new DeepSeekConnection(http));
    Check(await provider.GenerateAsync(AiProviderIds.DeepSeek, "deepseek-test-secret", "deepseek-flash", "Be clear", "Article text") == "DeepSeek response", "DeepSeek response is returned through provider dispatcher without exposing reasoning content");
}

var deepSeekSecret = "deepseek-must-not-leak";
using (var http = new HttpClient(new StubHandler((_, _) => JsonResponse(HttpStatusCode.Unauthorized, $"{{\"message\":\"invalid key {deepSeekSecret}\"}}"))))
{
    try
    {
        await new DeepSeekConnection(http).TestAsync(deepSeekSecret);
        throw new InvalidOperationException("Expected a DeepSeek authentication error.");
    }
    catch (InvalidOperationException exception)
    {
        Check(!exception.Message.Contains(deepSeekSecret, StringComparison.Ordinal), "DeepSeek API key is redacted from error details");
    }
}

Console.WriteLine($"AiProviderSmoke: {checks} checks passed.");

static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) => new(status)
{
    Content = new StringContent(json, Encoding.UTF8, "application/json")
};

sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(respond(request, cancellationToken));
}
