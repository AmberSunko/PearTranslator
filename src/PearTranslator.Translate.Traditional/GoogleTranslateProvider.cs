namespace PearTranslator.Translate.Traditional;

public sealed class GoogleTranslateProvider : TraditionalTranslationProviderBase, PearTranslator.Core.Abstractions.ITranslationProviderMetadata
{
    public GoogleTranslateProvider(HttpClient httpClient, TraditionalTranslationOptions options)
        : base(httpClient, options)
    {
    }

    public string ProviderLabel => "Google";

    public override async Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return string.Empty;
        }

        var uri = AppendQuery(Options.Endpoint, new Dictionary<string, string>
        {
            ["key"] = Options.ApiKey,
            ["q"] = sourceText,
            ["target"] = ToGoogleTargetLanguage(Options.TargetLanguage),
            ["format"] = "text"
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        var body = await ReadSuccessBodyOrThrowAsync(response, "Google Cloud Translation", cancellationToken);
        using var document = System.Text.Json.JsonDocument.Parse(body);
        var text = document.RootElement
            .GetProperty("data")
            .GetProperty("translations")[0]
            .GetProperty("translatedText")
            .GetString();
        return RequireTranslation("Google Cloud Translation", text);
    }

    private static string ToGoogleTargetLanguage(PearTranslator.Core.Configuration.TargetLanguage targetLanguage)
    {
        return targetLanguage switch
        {
            PearTranslator.Core.Configuration.TargetLanguage.English => "en",
            _ => "zh-CN"
        };
    }
}
