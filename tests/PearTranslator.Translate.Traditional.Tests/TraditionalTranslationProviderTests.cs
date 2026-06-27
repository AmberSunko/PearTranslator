using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PearTranslator.Core.Configuration;
using PearTranslator.Translate.Traditional;

namespace PearTranslator.Translate.Traditional.Tests;

public sealed class TraditionalTranslationProviderTests
{
    [Fact]
    public async Task AzureProviderSendsExpectedRequestAndParsesTranslation()
    {
        using var response = JsonResponse("""[{"translations":[{"text":"你好","to":"zh-Hans"}]}]""");
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new AzureTranslatorProvider(
            httpClient,
            new TraditionalTranslationOptions(
                "azure-key",
                new Uri("https://api.cognitive.microsofttranslator.com/"),
                Region: "eastasia"));

        var translated = await provider.TranslateAsync("Hello", CancellationToken.None);

        Assert.Equal("你好", translated);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal("/translate", handler.LastRequest?.RequestUri?.AbsolutePath);
        Assert.Contains("api-version=3.0", handler.LastRequest?.RequestUri?.Query);
        Assert.Contains("to=zh-Hans", handler.LastRequest?.RequestUri?.Query);
        Assert.Equal("azure-key", handler.LastRequest?.Headers.GetValues("Ocp-Apim-Subscription-Key").Single());
        Assert.Equal("eastasia", handler.LastRequest?.Headers.GetValues("Ocp-Apim-Subscription-Region").Single());
        Assert.Contains("\"Text\":\"Hello\"", handler.LastContent);
    }

    [Fact]
    public async Task DeepLProviderSendsExpectedRequestAndParsesTranslation()
    {
        using var response = JsonResponse("""{"translations":[{"detected_source_language":"EN","text":"你好"}]}""");
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new DeepLTranslatorProvider(
            httpClient,
            new TraditionalTranslationOptions("deepl-key", new Uri("https://api-free.deepl.com/v2/")));

        var translated = await provider.TranslateAsync("Hello", CancellationToken.None);

        Assert.Equal("你好", translated);
        Assert.Equal(new Uri("https://api-free.deepl.com/v2/translate"), handler.LastRequest?.RequestUri);
        Assert.Equal(new AuthenticationHeaderValue("DeepL-Auth-Key", "deepl-key"), handler.LastRequest?.Headers.Authorization);
        using (var payload = JsonDocument.Parse(handler.LastContent))
        {
            Assert.Equal("Hello", payload.RootElement.GetProperty("text")[0].GetString());
            Assert.False(payload.RootElement.TryGetProperty("source_lang", out _));
            Assert.Equal("ZH", payload.RootElement.GetProperty("target_lang").GetString());
        }
    }

    [Fact]
    public async Task GoogleProviderSendsExpectedRequestAndParsesTranslation()
    {
        using var response = JsonResponse("""{"data":{"translations":[{"translatedText":"你好"}]}}""");
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new GoogleTranslateProvider(
            httpClient,
            new TraditionalTranslationOptions("google-key", new Uri("https://translation.googleapis.com/language/translate/v2")));

        var translated = await provider.TranslateAsync("Hello", CancellationToken.None);

        Assert.Equal("你好", translated);
        Assert.Equal("/language/translate/v2", handler.LastRequest?.RequestUri?.AbsolutePath);
        Assert.Contains("key=google-key", handler.LastRequest?.RequestUri?.Query);
        Assert.Contains("q=Hello", handler.LastRequest?.RequestUri?.Query);
        Assert.DoesNotContain("source=", handler.LastRequest?.RequestUri?.Query);
        Assert.Contains("target=zh-CN", handler.LastRequest?.RequestUri?.Query);
        Assert.Contains("format=text", handler.LastRequest?.RequestUri?.Query);
        Assert.Equal(string.Empty, handler.LastContent);
    }

    [Fact]
    public async Task AzureProviderUsesEnglishTargetLanguage()
    {
        using var response = JsonResponse("""[{"translations":[{"text":"Hello","to":"en"}]}]""");
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new AzureTranslatorProvider(
            httpClient,
            new TraditionalTranslationOptions(
                "azure-key",
                new Uri("https://api.cognitive.microsofttranslator.com/"),
                TargetLanguage: TargetLanguage.English));

        await provider.TranslateAsync("你好", CancellationToken.None);

        Assert.DoesNotContain("from=", handler.LastRequest?.RequestUri?.Query);
        Assert.Contains("to=en", handler.LastRequest?.RequestUri?.Query);
    }

    [Fact]
    public async Task DeepLProviderUsesEnglishTargetLanguage()
    {
        using var response = JsonResponse("""{"translations":[{"detected_source_language":"ZH","text":"Hello"}]}""");
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new DeepLTranslatorProvider(
            httpClient,
            new TraditionalTranslationOptions(
                "deepl-key",
                new Uri("https://api-free.deepl.com/v2/"),
                TargetLanguage: TargetLanguage.English));

        await provider.TranslateAsync("你好", CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.LastContent);
        Assert.False(payload.RootElement.TryGetProperty("source_lang", out _));
        Assert.Equal("EN", payload.RootElement.GetProperty("target_lang").GetString());
    }

    [Fact]
    public async Task GoogleProviderUsesEnglishTargetLanguage()
    {
        using var response = JsonResponse("""{"data":{"translations":[{"translatedText":"Hello"}]}}""");
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new GoogleTranslateProvider(
            httpClient,
            new TraditionalTranslationOptions(
                "google-key",
                new Uri("https://translation.googleapis.com/language/translate/v2"),
                TargetLanguage: TargetLanguage.English));

        await provider.TranslateAsync("你好", CancellationToken.None);

        Assert.DoesNotContain("source=", handler.LastRequest?.RequestUri?.Query);
        Assert.Contains("target=en", handler.LastRequest?.RequestUri?.Query);
    }

    [Fact]
    public async Task ProviderThrowsReadableExceptionForHttpError()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"message":"bad key"}""", Encoding.UTF8, "application/json")
        };
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var provider = new DeepLTranslatorProvider(
            httpClient,
            new TraditionalTranslationOptions("bad-key", new Uri("https://api-free.deepl.com/v2/")));

        var exception = await Assert.ThrowsAsync<TraditionalTranslationException>(
            () => provider.TranslateAsync("Hello", CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Contains("bad key", exception.Message);
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
