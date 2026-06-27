using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PearTranslator.Translate.Traditional;

public sealed class AzureTranslatorProvider : TraditionalTranslationProviderBase, PearTranslator.Core.Abstractions.ITranslationProviderMetadata
{
    private static readonly JsonSerializerOptions AzureSerializerOptions = new()
    {
        PropertyNamingPolicy = null
    };

    public AzureTranslatorProvider(HttpClient httpClient, TraditionalTranslationOptions options)
        : base(httpClient, options)
    {
    }

    public string ProviderLabel => "Azure";

    public override async Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return string.Empty;
        }

        var uri = AppendQuery(AppendPath(Options.Endpoint, "translate"), new Dictionary<string, string>
        {
            ["api-version"] = "3.0",
            ["to"] = ToAzureTargetLanguage(Options.TargetLanguage)
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new[] { new AzureTranslationRequest(sourceText) }, AzureSerializerOptions),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("Ocp-Apim-Subscription-Key", Options.ApiKey);
        if (!string.IsNullOrWhiteSpace(Options.Region))
        {
            request.Headers.Add("Ocp-Apim-Subscription-Region", Options.Region);
        }

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        var body = await ReadSuccessBodyOrThrowAsync(response, "Azure Translator", cancellationToken);
        using var document = JsonDocument.Parse(body);
        var text = document.RootElement[0].GetProperty("translations")[0].GetProperty("text").GetString();
        return RequireTranslation("Azure Translator", text);
    }

    private static string ToAzureTargetLanguage(PearTranslator.Core.Configuration.TargetLanguage targetLanguage)
    {
        return targetLanguage switch
        {
            PearTranslator.Core.Configuration.TargetLanguage.English => "en",
            _ => "zh-Hans"
        };
    }

    private sealed record AzureTranslationRequest([property: JsonPropertyName("Text")] string Text);
}
