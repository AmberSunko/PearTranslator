namespace PearTranslator.Core.Configuration;

public sealed class TranslatorSettings
{
    public TranslationSettings Translation { get; init; } = new();

    public OverlaySettings Overlay { get; init; } = new();

    public OcrSettings Ocr { get; init; } = new();

    public AppearanceSettings Appearance { get; init; } = new();
}

public sealed class AppearanceSettings
{
    public UiLanguage UiLanguage { get; init; } = UiLanguage.SimplifiedChinese;
}

public enum UiLanguage
{
    SimplifiedChinese,
    English
}

public sealed class OverlaySettings
{
    public bool PositionOverlay { get; init; }

    public bool OcrPositionTest { get; init; }

    public bool LocalPreviewEnabled { get; init; } = true;

    public bool ExcludeOverlayFromCapture { get; init; } = true;

    public int OneShotDisplaySeconds { get; init; }
}

public sealed class OcrSettings
{
    public OcrEngineKind Engine { get; init; } = OcrEngineKind.LocalRapidOcr;

    public OcrLanguageKind Language { get; init; } = OcrLanguageKind.English;
}

public enum OcrEngineKind
{
    Windows,
    LocalRapidOcr
}

public enum OcrLanguageKind
{
    Auto,
    English,
    Chinese,
    Japanese,
    Korean
}

public sealed class TranslationSettings
{
    public TranslationProviderKind Provider { get; init; } = TranslationProviderKind.None;

    public TargetLanguage TargetLanguage { get; init; } = TargetLanguage.SimplifiedChinese;

    public OpenAiProviderSettings OpenAi { get; init; } = new();

    public TraditionalProviderSettings Azure { get; init; } = new();

    public TraditionalProviderSettings DeepL { get; init; } = new();

    public TraditionalProviderSettings Google { get; init; } = new();
}

public sealed class OpenAiProviderSettings
{
    public const string DefaultModel = "gpt-5.4-mini";
    public const string CustomModelValue = "custom";
    public const string DefaultBaseUri = "https://api.openai.com/v1/";
    public const string DeepSeekBaseUri = "https://api.deepseek.com/";
    public const string QwenBaseUri = "https://dashscope.aliyuncs.com/compatible-mode/v1/";
    public const string KimiBaseUri = "https://api.moonshot.ai/v1/";
    public const string ZhipuBaseUri = "https://open.bigmodel.cn/api/paas/v4/";
    public const string DoubaoBaseUri = "https://ark.cn-beijing.volces.com/api/v3/";

    public static IReadOnlyList<string> ModelPresets { get; } =
    [
        "gpt-5.4-nano",
        "gpt-5.4-mini",
        "gpt-5.4",
        "gpt-5.5",
        CustomModelValue
    ];

    public OpenAiCompatibleService Service { get; init; } = OpenAiCompatibleService.OpenAi;

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = DefaultModel;

    public string CustomModel { get; init; } = string.Empty;

    public string BaseUri { get; init; } = DefaultBaseUri;

    public string EffectiveModel
    {
        get
        {
            if (string.Equals(Model, CustomModelValue, StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(CustomModel)
                    ? GetDefaultModel(Service)
                    : CustomModel.Trim();
            }

            return string.IsNullOrWhiteSpace(Model) ? GetDefaultModel(Service) : Model.Trim();
        }
    }

    public string EffectiveBaseUri => ResolveBaseUri(Service, BaseUri);

    public static IReadOnlyList<string> GetModelPresets(OpenAiCompatibleService service)
    {
        return service switch
        {
            OpenAiCompatibleService.DeepSeek =>
            [
                "deepseek-chat",
                "deepseek-reasoner",
                "deepseek-v4-flash",
                "deepseek-v4-pro",
                CustomModelValue
            ],
            OpenAiCompatibleService.Qwen =>
            [
                "qwen-mt-flash",
                "qwen-mt-plus",
                "qwen-mt-lite",
                "qwen-mt-turbo",
                "qwen-plus-latest",
                "qwen-turbo-latest",
                "qwen-max-latest",
                "qwen3-plus",
                "qwen3-max",
                "qwen3-coder-plus",
                CustomModelValue
            ],
            OpenAiCompatibleService.Kimi =>
            [
                "moonshot-v1-8k",
                "moonshot-v1-32k",
                "moonshot-v1-128k",
                "kimi-k2.6",
                "kimi-k2-turbo-preview",
                "kimi-k2-0905-preview",
                CustomModelValue
            ],
            OpenAiCompatibleService.Zhipu =>
            [
                "glm-4-flash",
                "glm-4-air",
                "glm-4-plus",
                "glm-4.7-flashx",
                "glm-4.7",
                "glm-4.6",
                "glm-4.5",
                "glm-z1-air",
                CustomModelValue
            ],
            OpenAiCompatibleService.Doubao =>
            [
                "doubao-seed-1-6-flash-250615",
                "doubao-seed-1-6-250615",
                "doubao-seed-1-6-thinking-250615",
                "doubao-1-5-pro-32k-250115",
                "doubao-1-5-pro-256k-250115",
                "doubao-1-5-lite-32k-250115",
                "doubao-1-5-thinking-pro-250415",
                CustomModelValue
            ],
            OpenAiCompatibleService.Custom =>
            [
                CustomModelValue
            ],
            _ => ModelPresets
        };
    }

    public static string GetDefaultModel(OpenAiCompatibleService service)
    {
        return service switch
        {
            OpenAiCompatibleService.DeepSeek => "deepseek-chat",
            OpenAiCompatibleService.Qwen => "qwen-mt-flash",
            OpenAiCompatibleService.Kimi => "moonshot-v1-8k",
            OpenAiCompatibleService.Zhipu => "glm-4-flash",
            OpenAiCompatibleService.Doubao => "doubao-seed-1-6-flash-250615",
            _ => DefaultModel
        };
    }

    public static string GetDefaultBaseUri(OpenAiCompatibleService service)
    {
        return service switch
        {
            OpenAiCompatibleService.DeepSeek => DeepSeekBaseUri,
            OpenAiCompatibleService.Qwen => QwenBaseUri,
            OpenAiCompatibleService.Kimi => KimiBaseUri,
            OpenAiCompatibleService.Zhipu => ZhipuBaseUri,
            OpenAiCompatibleService.Doubao => DoubaoBaseUri,
            _ => DefaultBaseUri
        };
    }

    private static string ResolveBaseUri(OpenAiCompatibleService service, string configuredBaseUri)
    {
        if (service is OpenAiCompatibleService.Custom or OpenAiCompatibleService.OpenAi)
        {
            return NormalizeBaseUri(configuredBaseUri, DefaultBaseUri);
        }

        return GetDefaultBaseUri(service);
    }

    private static string NormalizeBaseUri(string value, string fallback)
    {
        var uri = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return uri.EndsWith("/", StringComparison.Ordinal) ? uri : uri + "/";
    }
}

public enum OpenAiCompatibleService
{
    OpenAi,
    DeepSeek,
    Qwen,
    Kimi,
    Zhipu,
    Doubao,
    Custom
}

public enum TargetLanguage
{
    SimplifiedChinese,
    English
}

public sealed class TraditionalProviderSettings
{
    public string ApiKey { get; init; } = string.Empty;

    public string Endpoint { get; init; } = string.Empty;

    public string Region { get; init; } = string.Empty;

    public string Project { get; init; } = string.Empty;
}
