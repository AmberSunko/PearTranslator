using PearTranslator.Core.Configuration;

namespace PearTranslator.Core.Tests.Configuration;

public sealed class TranslatorSettingsTests
{
    [Fact]
    public void DefaultsToOcrPreviewAndRecommendedOpenAiModel()
    {
        var settings = new TranslatorSettings();

        Assert.Equal(TranslationProviderKind.None, settings.Translation.Provider);
        Assert.Equal(TargetLanguage.SimplifiedChinese, settings.Translation.TargetLanguage);
        Assert.Equal("gpt-5.4-mini", settings.Translation.OpenAi.EffectiveModel);
        Assert.Equal("https://api.openai.com/v1/", settings.Translation.OpenAi.EffectiveBaseUri);
        Assert.False(settings.Overlay.PositionOverlay);
        Assert.False(settings.Overlay.OcrPositionTest);
        Assert.True(settings.Overlay.LocalPreviewEnabled);
        Assert.True(settings.Overlay.ExcludeOverlayFromCapture);
        Assert.Equal(0, settings.Overlay.OneShotDisplaySeconds);
        Assert.Equal(OcrEngineKind.LocalRapidOcr, settings.Ocr.Engine);
        Assert.Equal(OcrLanguageKind.English, settings.Ocr.Language);
        Assert.Equal(UiLanguage.SimplifiedChinese, settings.Appearance.UiLanguage);
    }

    [Fact]
    public void DeepSeekPresetUsesDomesticEndpointAndDefaultModel()
    {
        var settings = new OpenAiProviderSettings
        {
            Service = OpenAiCompatibleService.DeepSeek,
            Model = string.Empty,
            BaseUri = "https://ignored.example/v1/"
        };

        Assert.Equal("deepseek-chat", settings.EffectiveModel);
        Assert.Equal("https://api.deepseek.com/", settings.EffectiveBaseUri);
    }

    [Fact]
    public void CustomCompatibleServiceUsesConfiguredEndpointAndModel()
    {
        var settings = new OpenAiProviderSettings
        {
            Service = OpenAiCompatibleService.Custom,
            Model = OpenAiProviderSettings.CustomModelValue,
            CustomModel = " custom-model ",
            BaseUri = "https://compatible.example/v1"
        };

        Assert.Equal("custom-model", settings.EffectiveModel);
        Assert.Equal("https://compatible.example/v1/", settings.EffectiveBaseUri);
    }

    [Theory]
    [InlineData(OpenAiCompatibleService.DeepSeek, "deepseek-v4-flash")]
    [InlineData(OpenAiCompatibleService.DeepSeek, "deepseek-v4-pro")]
    [InlineData(OpenAiCompatibleService.Qwen, "qwen-mt-flash")]
    [InlineData(OpenAiCompatibleService.Qwen, "qwen-mt-plus")]
    [InlineData(OpenAiCompatibleService.Qwen, "qwen-mt-lite")]
    [InlineData(OpenAiCompatibleService.Kimi, "kimi-k2.6")]
    [InlineData(OpenAiCompatibleService.Kimi, "moonshot-v1-8k")]
    [InlineData(OpenAiCompatibleService.Zhipu, "glm-4.7")]
    [InlineData(OpenAiCompatibleService.Zhipu, "glm-4.7-flashx")]
    [InlineData(OpenAiCompatibleService.Doubao, "doubao-seed-1-6-thinking-250615")]
    [InlineData(OpenAiCompatibleService.Doubao, "doubao-seed-1-6-flash-250615")]
    public void DomesticModelPresetsIncludeTranslationSuitableModels(OpenAiCompatibleService service, string model)
    {
        Assert.Contains(model, OpenAiProviderSettings.GetModelPresets(service));
    }

    [Fact]
    public void CustomOpenAiModelOverridesSelectedPreset()
    {
        var settings = new OpenAiProviderSettings
        {
            Model = OpenAiProviderSettings.CustomModelValue,
            CustomModel = " gpt-custom "
        };

        Assert.Equal("gpt-custom", settings.EffectiveModel);
    }

    [Fact]
    public void InvalidOpenAiCustomModelFallsBackToDefault()
    {
        var settings = new OpenAiProviderSettings
        {
            Model = OpenAiProviderSettings.CustomModelValue,
            CustomModel = " "
        };

        Assert.Equal(OpenAiProviderSettings.DefaultModel, settings.EffectiveModel);
    }

    [Fact]
    public void SettingsRoundTripAsReadableJson()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pear-settings-{Guid.NewGuid():N}", "settings.json");
        var store = new TranslatorSettingsStore(path);
        var settings = new TranslatorSettings
        {
            Translation = new TranslationSettings
            {
                Provider = TranslationProviderKind.OpenAi,
                TargetLanguage = TargetLanguage.English,
                OpenAi = new OpenAiProviderSettings
                {
                    ApiKey = "sk-test",
                    Model = OpenAiProviderSettings.CustomModelValue,
                    CustomModel = "gpt-custom",
                    BaseUri = "https://example.test/v1/"
                },
                Azure = new TraditionalProviderSettings { ApiKey = "azure-key", Region = "eastasia" },
                DeepL = new TraditionalProviderSettings { ApiKey = "deepl-key", Endpoint = "https://api-free.deepl.com" },
                Google = new TraditionalProviderSettings { ApiKey = "google-key", Project = "project-id" }
            },
            Overlay = new OverlaySettings
            {
                PositionOverlay = true,
                OcrPositionTest = true,
                LocalPreviewEnabled = false,
                ExcludeOverlayFromCapture = false,
                OneShotDisplaySeconds = 30
            },
            Ocr = new OcrSettings
            {
                Engine = OcrEngineKind.LocalRapidOcr,
                Language = OcrLanguageKind.Korean
            },
            Appearance = new AppearanceSettings
            {
                UiLanguage = UiLanguage.English
            }
        };

        store.Save(settings);
        var loaded = store.Load();
        var json = File.ReadAllText(path);

        Assert.Contains("\"provider\": \"OpenAi\"", json);
        Assert.Equal(TranslationProviderKind.OpenAi, loaded.Translation.Provider);
        Assert.Equal(TargetLanguage.English, loaded.Translation.TargetLanguage);
        Assert.Equal("sk-test", loaded.Translation.OpenAi.ApiKey);
        Assert.Equal("gpt-custom", loaded.Translation.OpenAi.CustomModel);
        Assert.Equal("eastasia", loaded.Translation.Azure.Region);
        Assert.Equal("https://api-free.deepl.com", loaded.Translation.DeepL.Endpoint);
        Assert.Equal("project-id", loaded.Translation.Google.Project);
        Assert.True(loaded.Overlay.PositionOverlay);
        Assert.True(loaded.Overlay.OcrPositionTest);
        Assert.False(loaded.Overlay.LocalPreviewEnabled);
        Assert.False(loaded.Overlay.ExcludeOverlayFromCapture);
        Assert.Equal(30, loaded.Overlay.OneShotDisplaySeconds);
        Assert.Equal(OcrEngineKind.LocalRapidOcr, loaded.Ocr.Engine);
        Assert.Equal(OcrLanguageKind.Korean, loaded.Ocr.Language);
        Assert.Equal(UiLanguage.English, loaded.Appearance.UiLanguage);
        Assert.Contains("\"positionOverlay\": true", json);
        Assert.Contains("\"ocrPositionTest\": true", json);
        Assert.Contains("\"localPreviewEnabled\": false", json);
        Assert.Contains("\"excludeOverlayFromCapture\": false", json);
        Assert.Contains("\"oneShotDisplaySeconds\": 30", json);
        Assert.Contains("\"targetLanguage\": \"English\"", json);
        Assert.Contains("\"engine\": \"LocalRapidOcr\"", json);
        Assert.Contains("\"language\": \"Korean\"", json);
        Assert.Contains("\"uiLanguage\": \"English\"", json);
    }

    [Fact]
    public void MissingOrInvalidSettingsFileLoadsDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pear-settings-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "settings.json");
        var store = new TranslatorSettingsStore(path);

        var missing = store.Load();
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "{ nope");
        var invalid = store.Load();

        Assert.Equal(TranslationProviderKind.None, missing.Translation.Provider);
        Assert.Equal(TranslationProviderKind.None, invalid.Translation.Provider);
        Assert.Equal(OcrEngineKind.LocalRapidOcr, missing.Ocr.Engine);
        Assert.Equal(OcrEngineKind.LocalRapidOcr, invalid.Ocr.Engine);
    }
}
