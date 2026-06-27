using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace PearTranslator.Translate.OpenAI.Tests;

public sealed class OpenAiCompatibleModelCatalogClientTests
{
    [Fact]
    public async Task ListModelsSendsBearerRequestAndParsesModelIds()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "data": [
                    { "id": "qwen-mt-flash" },
                    { "id": "qwen-mt-plus" },
                    { "id": "qwen-mt-flash" },
                    { "id": " " }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        };
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var client = new OpenAiCompatibleModelCatalogClient(httpClient);

        var models = await client.ListModelsAsync(
            new OpenAiTranslationOptions("sk-test", "qwen-mt-flash", new Uri("https://example.test/v1/")),
            CancellationToken.None);

        Assert.Equal(["qwen-mt-flash", "qwen-mt-plus"], models);
        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal(new Uri("https://example.test/v1/models"), handler.LastRequest?.RequestUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "sk-test"), handler.LastRequest?.Headers.Authorization);
    }

    [Fact]
    public async Task ListModelsThrowsReadableExceptionForHttpError()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                """{"error":{"message":"bad key"}}""",
                Encoding.UTF8,
                "application/json")
        };
        var handler = new CapturingHandler(response);
        using var httpClient = new HttpClient(handler);
        var client = new OpenAiCompatibleModelCatalogClient(httpClient);

        var exception = await Assert.ThrowsAsync<OpenAiTranslationException>(
            () => client.ListModelsAsync(
                new OpenAiTranslationOptions("sk-test", "qwen-mt-flash", new Uri("https://example.test/v1/")),
                CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Contains("bad key", exception.Message);
    }

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }
    }
}
