using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PearTranslator.Translate.Traditional;

public sealed class DeepLTranslatorProvider : TraditionalTranslationProviderBase, PearTranslator.Core.Abstractions.ITranslationProviderMetadata
{
    public DeepLTranslatorProvider(HttpClient httpClient, TraditionalTranslationOptions options)
        : base(httpClient, options)
    {
    }

    public string ProviderLabel => "DeepL";

    public override async Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return string.Empty;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, AppendPath(Options.Endpoint, "translate"))
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    text = new[] { sourceText },
                    target_lang = ToDeepLTargetLanguage(Options.TargetLanguage)
                }, SerializerOptions),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", Options.ApiKey);

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        var body = await ReadSuccessBodyOrThrowAsync(response, "DeepL", cancellationToken);
        using var document = System.Text.Json.JsonDocument.Parse(body);
        var text = document.RootElement.GetProperty("translations")[0].GetProperty("text").GetString();
        return RequireTranslation("DeepL", text);
    }

    private static string ToDeepLTargetLanguage(PearTranslator.Core.Configuration.TargetLanguage targetLanguage)
    {
        return targetLanguage switch
        {
            PearTranslator.Core.Configuration.TargetLanguage.English => "EN",
            _ => "ZH"
        };
    }
}
