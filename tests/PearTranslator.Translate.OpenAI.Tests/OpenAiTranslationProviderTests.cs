using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PearTranslator.Core.Configuration;
using PearTranslator.Translate.OpenAI;

namespace PearTranslator.Translate.OpenAI.Tests;

public sealed class OpenAiTranslationProviderTests
{
    [Fact]
    public async Task SendsChatCompletionRequestAndReturnsTrimmedMessageContent()
    {
        using var response = JsonResponse("""
            {
              "choices": [
                {
                  "message": {
                    "content": "  你好，世界  "
                  }
                }
              ]
            }
            """);
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiTranslationProvider(
            httpClient,
            new OpenAiTranslationOptions(
                "sk-test",
                "gpt-test",
                new Uri("https://example.test/v1/")));

        var translation = await provider.TranslateAsync("Hello, world", CancellationToken.None);

        Assert.Equal("你好，世界", translation);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal(new Uri("https://example.test/v1/chat/completions"), handler.LastRequest?.RequestUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "sk-test"), handler.LastRequest?.Headers.Authorization);

        using var payload = JsonDocument.Parse(handler.LastContent);
        var root = payload.RootElement;
        Assert.Equal("gpt-test", root.GetProperty("model").GetString());
        var messages = root.GetProperty("messages");
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Contains("Simplified Chinese", messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("Hello, world", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task QwenMtModelsUseTranslationOptionsAndSingleUserMessage()
    {
        using var response = JsonResponse("""
            {
              "choices": [
                {
                  "message": {
                    "content": "你好，世界"
                  }
                }
              ]
            }
            """);
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiTranslationProvider(
            httpClient,
            new OpenAiTranslationOptions(
                "sk-test",
                "qwen-mt-flash",
                new Uri("https://example.test/v1/")));

        await provider.TranslateAsync("Hello, world", CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.LastContent);
        var root = payload.RootElement;
        Assert.Equal("qwen-mt-flash", root.GetProperty("model").GetString());
        var messages = root.GetProperty("messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        Assert.Equal("Hello, world", messages[0].GetProperty("content").GetString());
        Assert.Equal("auto", root.GetProperty("translation_options").GetProperty("source_lang").GetString());
        Assert.Equal("Chinese", root.GetProperty("translation_options").GetProperty("target_lang").GetString());
    }

    [Fact]
    public async Task EnglishTargetLanguageUsesEnglishTargetPrompt()
    {
        using var response = JsonResponse("""
            {
              "choices": [
                {
                  "message": {
                    "content": "Hello, world"
                  }
                }
              ]
            }
            """);
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiTranslationProvider(
            httpClient,
            new OpenAiTranslationOptions(
                "sk-test",
                "gpt-test",
                new Uri("https://example.test/v1/"),
                targetLanguage: TargetLanguage.English));

        await provider.TranslateAsync("你好，世界", CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.LastContent);
        var messages = payload.RootElement.GetProperty("messages");
        Assert.Contains("non-English", messages[0].GetProperty("content").GetString());
        Assert.Contains("natural English", messages[0].GetProperty("content").GetString());
        Assert.Equal("你好，世界", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task QwenMtModelsUseEnglishTargetTranslationOptions()
    {
        using var response = JsonResponse("""
            {
              "choices": [
                {
                  "message": {
                    "content": "Hello, world"
                  }
                }
              ]
            }
            """);
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiTranslationProvider(
            httpClient,
            new OpenAiTranslationOptions(
                "sk-test",
                "qwen-mt-flash",
                new Uri("https://example.test/v1/"),
                targetLanguage: TargetLanguage.English));

        await provider.TranslateAsync("你好，世界", CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.LastContent);
        var root = payload.RootElement;
        Assert.Equal("auto", root.GetProperty("translation_options").GetProperty("source_lang").GetString());
        Assert.Equal("English", root.GetProperty("translation_options").GetProperty("target_lang").GetString());
    }

    [Fact]
    public async Task DeepSeekV4ModelsDisableThinkingForLowLatencyTranslation()
    {
        using var response = JsonResponse("""
            {
              "choices": [
                {
                  "message": {
                    "content": "你好"
                  }
                }
              ]
            }
            """);
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiTranslationProvider(
            httpClient,
            new OpenAiTranslationOptions(
                "sk-test",
                "deepseek-v4-flash",
                new Uri("https://api.deepseek.com/")));

        await provider.TranslateAsync("Hello", CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.LastContent);
        var root = payload.RootElement;
        Assert.Equal("deepseek-v4-flash", root.GetProperty("model").GetString());
        Assert.Equal("disabled", root.GetProperty("thinking").GetProperty("type").GetString());
    }

    [Fact]
    public async Task ThrowsTranslationExceptionWhenApiReturnsError()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(
                """{"error":{"message":"rate limited"}}""",
                Encoding.UTF8,
                "application/json")
        };
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = CreateProvider(httpClient);

        var exception = await Assert.ThrowsAsync<OpenAiTranslationException>(
            () => provider.TranslateAsync("Hello", CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Contains("rate limited", exception.Message);
    }

    [Fact]
    public async Task ThrowsTranslationExceptionWhenResponseHasNoMessageContent()
    {
        using var response = JsonResponse("""{"choices":[]}""");
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = CreateProvider(httpClient);

        var exception = await Assert.ThrowsAsync<OpenAiTranslationException>(
            () => provider.TranslateAsync("Hello", CancellationToken.None));

        Assert.Contains("No translation content", exception.Message);
    }

    private static OpenAiTranslationProvider CreateProvider(HttpClient httpClient)
    {
        return new OpenAiTranslationProvider(
            httpClient,
            new OpenAiTranslationOptions(
                "sk-test",
                "gpt-test",
                new Uri("https://example.test/v1/")));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string LastContent { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastContent = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return response;
        }
    }
}
