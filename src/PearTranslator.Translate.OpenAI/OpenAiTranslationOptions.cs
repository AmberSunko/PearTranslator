using PearTranslator.Core.Configuration;

namespace PearTranslator.Translate.OpenAI;

public sealed record OpenAiTranslationOptions
{
    public const string DefaultModel = "gpt-5.4-mini";

    public static readonly Uri DefaultBaseUri = new("https://api.openai.com/v1/");

    public OpenAiTranslationOptions(string apiKey)
        : this(apiKey, DefaultModel, DefaultBaseUri)
    {
    }

    public OpenAiTranslationOptions(
        string apiKey,
        string model,
        Uri baseUri,
        bool useSystemProxy = true,
        TargetLanguage targetLanguage = TargetLanguage.SimplifiedChinese)
    {
        ApiKey = RequireValue(apiKey, nameof(apiKey));
        Model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();
        BaseUri = EnsureTrailingSlash(baseUri);
        UseSystemProxy = useSystemProxy;
        TargetLanguage = targetLanguage;
    }

    public string ApiKey { get; }

    public string Model { get; }

    public Uri BaseUri { get; }

    public bool UseSystemProxy { get; }

    public TargetLanguage TargetLanguage { get; }

    public static bool TryCreate(Func<string, string?> readSetting, out OpenAiTranslationOptions? options)
    {
        var apiKey = FirstValue(
            readSetting("PEAR_TRANSLATOR_OPENAI_API_KEY"),
            readSetting("OPENAI_API_KEY"));

        if (apiKey is null)
        {
            options = null;
            return false;
        }

        var model = FirstValue(readSetting("PEAR_TRANSLATOR_OPENAI_MODEL")) ?? DefaultModel;
        var baseUri = TryReadBaseUri(readSetting("PEAR_TRANSLATOR_OPENAI_BASE_URI"));
        options = new OpenAiTranslationOptions(apiKey, model, baseUri);
        return true;
    }

    public static bool TryCreate(
        OpenAiProviderSettings settings,
        Func<string, string?> readSetting,
        TargetLanguage targetLanguage,
        out OpenAiTranslationOptions? options)
    {
        var apiKey = FirstValue(
            settings.ApiKey,
            readSetting("PEAR_TRANSLATOR_OPENAI_API_KEY"),
            readSetting("OPENAI_API_KEY"));

        if (apiKey is null)
        {
            options = null;
            return false;
        }

        var model = FirstValue(
            ReadConfiguredModel(settings),
            readSetting("PEAR_TRANSLATOR_OPENAI_MODEL")) ??
            OpenAiProviderSettings.GetDefaultModel(settings.Service);
        var baseUri = TryReadBaseUri(
            FirstValue(ReadConfiguredBaseUri(settings), readSetting("PEAR_TRANSLATOR_OPENAI_BASE_URI")),
            OpenAiProviderSettings.GetDefaultBaseUri(settings.Service));

        options = new OpenAiTranslationOptions(
            apiKey,
            model,
            baseUri,
            useSystemProxy: ShouldUseSystemProxy(settings.Service),
            targetLanguage: targetLanguage);
        return true;
    }

    public static bool TryCreate(
        OpenAiProviderSettings settings,
        Func<string, string?> readSetting,
        out OpenAiTranslationOptions? options)
    {
        return TryCreate(settings, readSetting, TargetLanguage.SimplifiedChinese, out options);
    }

    private static bool ShouldUseSystemProxy(OpenAiCompatibleService service)
    {
        return service is OpenAiCompatibleService.OpenAi or OpenAiCompatibleService.Custom;
    }

    private static string? ReadConfiguredModel(OpenAiProviderSettings settings)
    {
        if (string.Equals(settings.Model, OpenAiProviderSettings.CustomModelValue, StringComparison.OrdinalIgnoreCase))
        {
            return FirstValue(settings.CustomModel);
        }

        return FirstValue(settings.Model);
    }

    private static string? ReadConfiguredBaseUri(OpenAiProviderSettings settings)
    {
        if (settings.Service is not (OpenAiCompatibleService.OpenAi or OpenAiCompatibleService.Custom))
        {
            return OpenAiProviderSettings.GetDefaultBaseUri(settings.Service);
        }

        return FirstValue(settings.BaseUri);
    }

    private static Uri TryReadBaseUri(string? rawValue)
    {
        return TryReadBaseUri(rawValue, DefaultBaseUri.AbsoluteUri);
    }

    private static Uri TryReadBaseUri(string? rawValue, string fallback)
    {
        var value = FirstValue(rawValue);
        return value is not null && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : new Uri(fallback);
    }

    private static string? FirstValue(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string RequireValue(string value, string name)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be blank.", name)
            : value.Trim();
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        if (uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal))
        {
            return uri;
        }

        return new Uri(uri.AbsoluteUri + "/");
    }
}
