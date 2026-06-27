using PearTranslator.Core.Configuration;
using PearTranslator.Core.Control;

namespace PearTranslator.App.Wpf;

public sealed class MainWindowTextCatalog
{
    private static readonly (string Chinese, string English)[] StaticTextPairs =
    [
        ("界面语言", "UI Language"),
        ("切换设置界面显示语言。", "Switch the settings interface language."),
        ("基础", "Basics"),
        ("翻译服务", "Translation Service"),
        ("目标语言", "Target Language"),
        ("只翻译不是目标语言的内容。", "Only translate content that is not already in the target language."),
        ("OCR 引擎", "OCR Engine"),
        ("识别屏幕文字的本地或系统 OCR 方案。", "Local or system OCR used to read screen text."),
        ("OCR 语言", "OCR Language"),
        ("影响本地模型和系统识别的语言偏好。", "Language preference for local models and system OCR."),
        ("本地资源", "Local Assets"),
        ("正在检查 OCR 模型和 ECDICT 字典。", "Checking OCR models and the ECDICT dictionary."),
        ("配置模型", "Configure Models"),
        ("单次显示时间", "One-shot Display Time"),
        ("单次截图覆盖层保留多久。", "How long a one-shot overlay stays visible."),
        ("大模型", "LLM"),
        ("模型平台", "Model Platform"),
        ("选择国内或 OpenAI-compatible 平台。", "Choose a domestic or OpenAI-compatible platform."),
        ("API 密钥", "API Key"),
        ("仅保存在本机配置里。", "Saved only in the local settings file."),
        ("模型", "Model"),
        ("可获取平台模型列表，也可选择自定义。", "Fetch the platform model list or choose a custom model."),
        ("获取模型", "Fetch Models"),
        ("自定义模型", "Custom Model"),
        ("填入模型 ID。", "Enter the model ID."),
        ("接口地址", "Base URL"),
        ("留空时使用平台默认地址。", "Leave empty to use the platform default endpoint."),
        ("传统翻译", "Traditional Translation"),
        ("传统翻译服务的认证信息。", "Credentials for the selected traditional translation service."),
        ("接口地址 / 区域 / 项目", "Endpoint / Region / Project"),
        ("不同服务会使用不同字段。", "Different services use different fields."),
        ("操作", "Actions"),
        ("保存翻译设置", "Save Translation Settings"),
        ("本地预翻译", "Local Preview"),
        ("不捕获覆盖层", "Exclude Overlay"),
        ("位置覆盖", "Position Overlay"),
        ("OCR位置测试", "OCR Position Test"),
        ("准备就绪。请选择字幕区域。", "Ready. Select a subtitle region."),
        ("选择区域", "Select Region"),
        ("单次截图", "One-shot"),
        ("暂停", "Pause"),
        ("继续", "Resume")
    ];

    private readonly Dictionary<string, string> _staticTextMap;

    private MainWindowTextCatalog(UiLanguage language)
    {
        Language = language;
        _staticTextMap = BuildStaticTextMap(language);
    }

    public UiLanguage Language { get; }

    public string UiLanguageTitle => Language == UiLanguage.English ? "UI Language" : "界面语言";

    public string SelectRegionButton => Language == UiLanguage.English ? "Select Region" : "选择区域";

    public string OneShotButton => Language == UiLanguage.English ? "One-shot" : "单次截图";

    public string PauseButton => Language == UiLanguage.English ? "Pause" : "暂停";

    public string ResumeButton => Language == UiLanguage.English ? "Resume" : "继续";

    public string ReadyStatus => Language == UiLanguage.English
        ? "Ready. Select a subtitle region."
        : "准备就绪。请选择字幕区域。";

    public static MainWindowTextCatalog For(UiLanguage language)
    {
        return new MainWindowTextCatalog(language);
    }

    public bool TryTranslateStaticText(string value, out string localized)
    {
        return _staticTextMap.TryGetValue(value, out localized!);
    }

    public string FormatShortcutSummary(string summary)
    {
        if (Language != UiLanguage.English)
        {
            return summary
                .Replace("Select Region:", "选择区域：", StringComparison.Ordinal)
                .Replace("Pause/Resume:", "暂停/继续：", StringComparison.Ordinal)
                .Replace("Hide Current:", "隐藏当前：", StringComparison.Ordinal)
                .Replace("One-shot:", "单次截图：", StringComparison.Ordinal)
                .Replace("Unregistered", "未注册", StringComparison.Ordinal);
        }

        return summary
            .Replace("选择区域：", "Select Region: ", StringComparison.Ordinal)
            .Replace("暂停/继续：", "Pause/Resume: ", StringComparison.Ordinal)
            .Replace("隐藏当前：", "Hide Current: ", StringComparison.Ordinal)
            .Replace("单次截图：", "One-shot: ", StringComparison.Ordinal)
            .Replace("未注册", "Unregistered", StringComparison.Ordinal);
    }

    public string ProviderHint(TranslationProviderKind provider)
    {
        if (Language == UiLanguage.English)
        {
            return provider switch
            {
                TranslationProviderKind.OpenAi => "Use an OpenAI-compatible API. Domestic providers have built-in endpoints; enter an API key and choose a model.",
                TranslationProviderKind.Azure => "Use Azure Translator. Enter the subscription key and region; endpoint is optional.",
                TranslationProviderKind.DeepL => "Use DeepL. Enter the authentication key; endpoint is optional.",
                TranslationProviderKind.Google => "Use Google Cloud Translation Basic. Enter an API key; endpoint is optional.",
                TranslationProviderKind.Mock => "Use the built-in debug translator.",
                _ => "Do not call remote translation; show recognized subtitle text only."
            };
        }

        return provider switch
        {
            TranslationProviderKind.OpenAi => "使用 OpenAI-compatible 接口。国内大模型已内置端点，填 API 密钥后选择模型即可。",
            TranslationProviderKind.Azure => "使用 Azure 翻译。请填写订阅密钥和区域；接口地址可选。",
            TranslationProviderKind.DeepL => "使用 DeepL。请填写认证密钥；接口地址可选。",
            TranslationProviderKind.Google => "使用 Google 云翻译基础版。请填写 API 密钥；接口地址可选。",
            TranslationProviderKind.Mock => "使用内置调试翻译。",
            _ => "不调用远程翻译，只显示识别出的字幕文本。"
        };
    }

    public string RuntimeAssetsStatus(bool isComplete, int missingCount)
    {
        if (Language == UiLanguage.English)
        {
            return isComplete
                ? "OCR models and the ECDICT dictionary are configured."
                : $"Missing {missingCount} local assets. Click the button to download and configure them.";
        }

        return isComplete
            ? "OCR 模型和 ECDICT 字典已配置。"
            : $"缺少 {missingCount} 个本地资源，点击按钮后自动下载配置。";
    }

    public string TraditionalProviderTitle(TranslationProviderKind provider)
    {
        if (Language == UiLanguage.English)
        {
            return provider switch
            {
                TranslationProviderKind.Azure => "Azure Subscription Key",
                TranslationProviderKind.DeepL => "DeepL Authentication Key",
                TranslationProviderKind.Google => "Google API Key",
                _ => "API Key"
            };
        }

        return provider switch
        {
            TranslationProviderKind.Azure => "Azure 订阅密钥",
            TranslationProviderKind.DeepL => "DeepL 认证密钥",
            TranslationProviderKind.Google => "Google API 密钥",
            _ => "API 密钥"
        };
    }

    public string RunState(TranslatorRunState state)
    {
        if (Language == UiLanguage.English)
        {
            return state switch
            {
                TranslatorRunState.Paused => "Paused",
                TranslatorRunState.Dismissed => "Current subtitles hidden",
                _ => "Running"
            };
        }

        return state switch
        {
            TranslatorRunState.Paused => "已暂停",
            TranslatorRunState.Dismissed => "已隐藏当前字幕",
            _ => "正在运行"
        };
    }

    public string PauseCommand(TranslatorRunState state)
    {
        return state == TranslatorRunState.Paused ? ResumeButton : PauseButton;
    }

    public string BuildProviderStatus(TranslatorSettings settings)
    {
        if (Language == UiLanguage.English)
        {
            return settings.Translation.Provider switch
            {
                TranslationProviderKind.OpenAi when string.IsNullOrWhiteSpace(settings.Translation.OpenAi.ApiKey) =>
                    $"Selected {ServiceLabel(settings.Translation.OpenAi.Service)}. Enter an API key or set OPENAI_API_KEY.",
                TranslationProviderKind.OpenAi =>
                    $"Selected {ServiceLabel(settings.Translation.OpenAi.Service)}: {settings.Translation.OpenAi.EffectiveModel}.",
                TranslationProviderKind.Mock =>
                    "Selected mock translation. Select a subtitle region.",
                TranslationProviderKind.Azure when string.IsNullOrWhiteSpace(settings.Translation.Azure.ApiKey) =>
                    "Selected Azure. Enter a subscription key.",
                TranslationProviderKind.DeepL when string.IsNullOrWhiteSpace(settings.Translation.DeepL.ApiKey) =>
                    "Selected DeepL. Enter an authentication key.",
                TranslationProviderKind.Google when string.IsNullOrWhiteSpace(settings.Translation.Google.ApiKey) =>
                    "Selected Google. Enter an API key.",
                TranslationProviderKind.Azure or TranslationProviderKind.DeepL or TranslationProviderKind.Google =>
                    $"Selected {settings.Translation.Provider}. Select a subtitle region.",
                _ =>
                    "Selected OCR preview. Select a subtitle region."
            };
        }

        return settings.Translation.Provider switch
        {
            TranslationProviderKind.OpenAi when string.IsNullOrWhiteSpace(settings.Translation.OpenAi.ApiKey) =>
                $"已选择 {ServiceLabel(settings.Translation.OpenAi.Service)}。请填写 API 密钥，或设置环境变量 OPENAI_API_KEY。",
            TranslationProviderKind.OpenAi =>
                $"已选择 {ServiceLabel(settings.Translation.OpenAi.Service)}：{settings.Translation.OpenAi.EffectiveModel}。",
            TranslationProviderKind.Mock =>
                "已选择模拟翻译。请选择字幕区域。",
            TranslationProviderKind.Azure when string.IsNullOrWhiteSpace(settings.Translation.Azure.ApiKey) =>
                "已选择 Azure。请填写订阅密钥。",
            TranslationProviderKind.DeepL when string.IsNullOrWhiteSpace(settings.Translation.DeepL.ApiKey) =>
                "已选择 DeepL。请填写认证密钥。",
            TranslationProviderKind.Google when string.IsNullOrWhiteSpace(settings.Translation.Google.ApiKey) =>
                "已选择 Google。请填写 API 密钥。",
            TranslationProviderKind.Azure or TranslationProviderKind.DeepL or TranslationProviderKind.Google =>
                $"已选择 {settings.Translation.Provider}。请选择字幕区域。",
            _ =>
                "已选择识别预览。请选择字幕区域。"
        };
    }

    public string WaitingReason(TargetLanguage targetLanguage)
    {
        if (Language == UiLanguage.English)
        {
            return targetLanguage == TargetLanguage.English
                ? "waiting for non-English subtitles"
                : "waiting for English subtitles";
        }

        return targetLanguage == TargetLanguage.English
            ? "等待非英文字幕"
            : "等待英文字幕";
    }

    public string ServiceLabel(OpenAiCompatibleService service)
    {
        return service switch
        {
            OpenAiCompatibleService.DeepSeek => "DeepSeek",
            OpenAiCompatibleService.Qwen => Language == UiLanguage.English ? "Qwen" : "通义千问 / Qwen",
            OpenAiCompatibleService.Kimi => "Moonshot / Kimi",
            OpenAiCompatibleService.Zhipu => Language == UiLanguage.English ? "Zhipu GLM" : "智谱 GLM",
            OpenAiCompatibleService.Doubao => Language == UiLanguage.English ? "Volcengine Ark / Doubao" : "火山方舟 / Doubao",
            OpenAiCompatibleService.Custom => Language == UiLanguage.English ? "Custom Compatible Endpoint" : "自定义兼容接口",
            _ => "OpenAI"
        };
    }

    public string ModelLabel(string model)
    {
        if (model == OpenAiProviderSettings.CustomModelValue)
        {
            return Language == UiLanguage.English ? "Custom..." : "自定义...";
        }

        if (Language != UiLanguage.English)
        {
            return model switch
            {
                "gpt-5.4-nano" => "gpt-5.4-nano（经济）",
                "gpt-5.4-mini" => "gpt-5.4-mini（推荐）",
                "gpt-5.4" => "gpt-5.4（高质量）",
                "gpt-5.5" => "gpt-5.5（最高质量）",
                "deepseek-chat" => "deepseek-chat（推荐）",
                "deepseek-reasoner" => "deepseek-reasoner（推理）",
                "deepseek-v4-flash" => "deepseek-v4-flash（低延迟翻译）",
                "deepseek-v4-pro" => "deepseek-v4-pro（高质量翻译）",
                "qwen-mt-flash" => "qwen-mt-flash（翻译专用，推荐）",
                "qwen-mt-plus" => "qwen-mt-plus（翻译专用，高质量）",
                "qwen-mt-lite" => "qwen-mt-lite（翻译专用，经济）",
                "qwen-mt-turbo" => "qwen-mt-turbo（翻译专用，高速）",
                "qwen-plus-latest" => "qwen-plus-latest（通用翻译）",
                "qwen-turbo-latest" => "qwen-turbo-latest（通用高速）",
                "qwen-max-latest" => "qwen-max-latest（通用高质量）",
                "qwen3-plus" => "qwen3-plus（通用翻译）",
                "qwen3-max" => "qwen3-max（通用高质量）",
                "qwen3-coder-plus" => "qwen3-coder-plus（代码游戏文本）",
                "moonshot-v1-8k" => "moonshot-v1-8k（推荐）",
                "moonshot-v1-32k" => "moonshot-v1-32k",
                "moonshot-v1-128k" => "moonshot-v1-128k",
                "kimi-k2.6" => "kimi-k2.6（高质量翻译）",
                "kimi-k2-turbo-preview" => "kimi-k2-turbo-preview（高质量高速）",
                "kimi-k2-0905-preview" => "kimi-k2-0905-preview（高质量）",
                "glm-4-flash" => "glm-4-flash（经济）",
                "glm-4-air" => "glm-4-air（均衡）",
                "glm-4-plus" => "glm-4-plus（高质量）",
                "glm-4.7-flashx" => "glm-4.7-flashx（低延迟翻译）",
                "glm-4.7" => "glm-4.7（高质量翻译）",
                "glm-4.6" => "glm-4.6（高质量翻译）",
                "glm-4.5" => "glm-4.5（高质量）",
                "glm-z1-air" => "glm-z1-air（推理）",
                "doubao-seed-1-6-flash-250615" => "doubao-seed-1-6-flash-250615（推荐）",
                "doubao-seed-1-6-250615" => "doubao-seed-1-6-250615（高质量）",
                "doubao-seed-1-6-thinking-250615" => "doubao-seed-1-6-thinking-250615（推理）",
                "doubao-1-5-pro-32k-250115" => "doubao-1-5-pro-32k-250115（通用翻译）",
                "doubao-1-5-pro-256k-250115" => "doubao-1-5-pro-256k-250115（长文本）",
                "doubao-1-5-lite-32k-250115" => "doubao-1-5-lite-32k-250115（经济）",
                "doubao-1-5-thinking-pro-250415" => "doubao-1-5-thinking-pro-250415（推理）",
                _ => model
            };
        }

        return model switch
        {
            "gpt-5.4-nano" => "gpt-5.4-nano (budget)",
            "gpt-5.4-mini" => "gpt-5.4-mini (recommended)",
            "gpt-5.4" => "gpt-5.4 (high quality)",
            "gpt-5.5" => "gpt-5.5 (highest quality)",
            "deepseek-chat" => "deepseek-chat (recommended)",
            "deepseek-reasoner" => "deepseek-reasoner (reasoning)",
            "deepseek-v4-flash" => "deepseek-v4-flash (low-latency translation)",
            "deepseek-v4-pro" => "deepseek-v4-pro (high-quality translation)",
            "qwen-mt-flash" => "qwen-mt-flash (translation, recommended)",
            "qwen-mt-plus" => "qwen-mt-plus (translation, high quality)",
            "qwen-mt-lite" => "qwen-mt-lite (translation, budget)",
            "qwen-mt-turbo" => "qwen-mt-turbo (translation, fast)",
            "qwen-plus-latest" => "qwen-plus-latest (general translation)",
            "qwen-turbo-latest" => "qwen-turbo-latest (general, fast)",
            "qwen-max-latest" => "qwen-max-latest (general, high quality)",
            "qwen3-plus" => "qwen3-plus (general translation)",
            "qwen3-max" => "qwen3-max (general, high quality)",
            "qwen3-coder-plus" => "qwen3-coder-plus (code/game text)",
            "moonshot-v1-8k" => "moonshot-v1-8k (recommended)",
            "kimi-k2.6" => "kimi-k2.6 (high-quality translation)",
            "kimi-k2-turbo-preview" => "kimi-k2-turbo-preview (high quality, fast)",
            "kimi-k2-0905-preview" => "kimi-k2-0905-preview (high quality)",
            "glm-4-flash" => "glm-4-flash (budget)",
            "glm-4-air" => "glm-4-air (balanced)",
            "glm-4-plus" => "glm-4-plus (high quality)",
            "glm-4.7-flashx" => "glm-4.7-flashx (low-latency translation)",
            "glm-4.7" => "glm-4.7 (high-quality translation)",
            "glm-4.6" => "glm-4.6 (high-quality translation)",
            "glm-4.5" => "glm-4.5 (high quality)",
            "glm-z1-air" => "glm-z1-air (reasoning)",
            "doubao-seed-1-6-flash-250615" => "doubao-seed-1-6-flash-250615 (recommended)",
            "doubao-seed-1-6-250615" => "doubao-seed-1-6-250615 (high quality)",
            "doubao-seed-1-6-thinking-250615" => "doubao-seed-1-6-thinking-250615 (reasoning)",
            "doubao-1-5-pro-32k-250115" => "doubao-1-5-pro-32k-250115 (general translation)",
            "doubao-1-5-pro-256k-250115" => "doubao-1-5-pro-256k-250115 (long context)",
            "doubao-1-5-lite-32k-250115" => "doubao-1-5-lite-32k-250115 (budget)",
            "doubao-1-5-thinking-pro-250415" => "doubao-1-5-thinking-pro-250415 (reasoning)",
            _ => model
        };
    }

    public string ProviderNone => Language == UiLanguage.English ? "No API / OCR Preview" : "不接 API / 识别预览";
    public string ProviderOpenAi => Language == UiLanguage.English ? "LLM / OpenAI Compatible" : "大模型 / OpenAI 兼容";
    public string ProviderAzure => Language == UiLanguage.English ? "Azure Translator" : "Azure 翻译";
    public string ProviderDeepL => "DeepL";
    public string ProviderGoogle => Language == UiLanguage.English ? "Google Cloud Translation" : "Google 云翻译";
    public string ProviderMock => Language == UiLanguage.English ? "Mock / Debug" : "模拟 / 调试";
    public string OcrEngineWindows => Language == UiLanguage.English ? "Windows Built-in" : "Windows 内置";
    public string OcrEngineLocal => Language == UiLanguage.English ? "Local High Accuracy" : "本地高精度";
    public string TargetSimplifiedChinese => Language == UiLanguage.English ? "Simplified Chinese" : "简体中文";
    public string TargetEnglish => Language == UiLanguage.English ? "English" : "英文";
    public string OcrAuto => Language == UiLanguage.English ? "Auto Detect" : "自动检测";
    public string OcrChinese => Language == UiLanguage.English ? "Chinese" : "中文";
    public string OcrJapanese => Language == UiLanguage.English ? "Japanese" : "日文";
    public string OcrKorean => Language == UiLanguage.English ? "Korean" : "韩文";
    public string OneShotKeep => Language == UiLanguage.English ? "Keep" : "保持";

    private static Dictionary<string, string> BuildStaticTextMap(UiLanguage language)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (chinese, english) in StaticTextPairs)
        {
            var localized = language == UiLanguage.English ? english : chinese;
            map[chinese] = localized;
            map[english] = localized;
        }

        return map;
    }
}
