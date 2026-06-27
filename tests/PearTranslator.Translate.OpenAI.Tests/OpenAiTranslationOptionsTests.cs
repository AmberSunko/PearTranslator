using PearTranslator.Translate.OpenAI;
using PearTranslator.Core.Configuration;

namespace PearTranslator.Translate.OpenAI.Tests;

public sealed class OpenAiTranslationOptionsTests
{
    [Fact]
    public void CreatesOptionsFromOpenAiApiKeyEnvironmentVariable()
    {
        var created = OpenAiTranslationOptions.TryCreate(ReadSetting, out var options);

        Assert.True(created);
        Assert.NotNull(options);
        Assert.Equal("sk-test", options.ApiKey);
        Assert.Equal("gpt-5.4-mini", options.Model);
        Assert.Equal(new Uri("https://api.openai.com/v1/"), options.BaseUri);
    }

    [Fact]
    public void PearTranslatorApiKeyOverridesGenericOpenAiApiKey()
    {
        var created = OpenAiTranslationOptions.TryCreate(key => key switch
        {
            "PEAR_TRANSLATOR_OPENAI_API_KEY" => "sk-pear",
            "OPENAI_API_KEY" => "sk-openai",
            _ => null
        }, out var options);

        Assert.True(created);
        Assert.Equal("sk-pear", options?.ApiKey);
    }

    [Fact]
    public void AllowsModelAndBaseUriOverrides()
    {
        var created = OpenAiTranslationOptions.TryCreate(key => key switch
        {
            "OPENAI_API_KEY" => "sk-test",
            "PEAR_TRANSLATOR_OPENAI_MODEL" => "gpt-custom",
            "PEAR_TRANSLATOR_OPENAI_BASE_URI" => "https://example.test/custom/",
            _ => null
        }, out var options);

        Assert.True(created);
        Assert.Equal("gpt-custom", options?.Model);
        Assert.Equal(new Uri("https://example.test/custom/"), options?.BaseUri);
    }

    [Fact]
    public void DoesNotCreateOptionsWithoutApiKey()
    {
        var created = OpenAiTranslationOptions.TryCreate(_ => null, out var options);

        Assert.False(created);
        Assert.Null(options);
    }

    [Fact]
    public void UiSettingsOverrideEnvironmentVariables()
    {
        var settings = new OpenAiProviderSettings
        {
            ApiKey = "sk-ui",
            Model = OpenAiProviderSettings.CustomModelValue,
            CustomModel = "gpt-ui",
            BaseUri = "https://ui.example/v1/"
        };

        var created = OpenAiTranslationOptions.TryCreate(settings, key => key switch
        {
            "OPENAI_API_KEY" => "sk-env",
            "PEAR_TRANSLATOR_OPENAI_MODEL" => "gpt-env",
            "PEAR_TRANSLATOR_OPENAI_BASE_URI" => "https://env.example/v1/",
            _ => null
        }, out var options);

        Assert.True(created);
        Assert.Equal("sk-ui", options?.ApiKey);
        Assert.Equal("gpt-ui", options?.Model);
        Assert.Equal(new Uri("https://ui.example/v1/"), options?.BaseUri);
    }

    [Fact]
    public void DomesticPresetSettingsUseProviderDefaults()
    {
        var settings = new OpenAiProviderSettings
        {
            ApiKey = "sk-deepseek",
            Service = OpenAiCompatibleService.DeepSeek,
            Model = string.Empty,
            BaseUri = "https://ignored.example/v1/"
        };

        var created = OpenAiTranslationOptions.TryCreate(settings, _ => null, out var options);

        Assert.True(created);
        Assert.Equal("sk-deepseek", options?.ApiKey);
        Assert.Equal("deepseek-chat", options?.Model);
        Assert.Equal(new Uri("https://api.deepseek.com/"), options?.BaseUri);
        Assert.False(options?.UseSystemProxy);
    }

    [Theory]
    [InlineData(OpenAiCompatibleService.DeepSeek)]
    [InlineData(OpenAiCompatibleService.Qwen)]
    [InlineData(OpenAiCompatibleService.Kimi)]
    [InlineData(OpenAiCompatibleService.Zhipu)]
    [InlineData(OpenAiCompatibleService.Doubao)]
    public void DomesticCompatibleServicesUseDirectConnectionByDefault(OpenAiCompatibleService service)
    {
        var settings = new OpenAiProviderSettings
        {
            ApiKey = "sk-test",
            Service = service
        };

        var created = OpenAiTranslationOptions.TryCreate(settings, _ => null, out var options);

        Assert.True(created);
        Assert.False(options?.UseSystemProxy);
    }

    [Theory]
    [InlineData(OpenAiCompatibleService.OpenAi)]
    [InlineData(OpenAiCompatibleService.Custom)]
    public void OpenAiAndCustomCompatibleServicesKeepSystemProxyByDefault(OpenAiCompatibleService service)
    {
        var settings = new OpenAiProviderSettings
        {
            ApiKey = "sk-test",
            Service = service,
            BaseUri = "https://example.test/v1/"
        };

        var created = OpenAiTranslationOptions.TryCreate(settings, _ => null, out var options);

        Assert.True(created);
        Assert.True(options?.UseSystemProxy);
    }

    [Fact]
    public void EnvironmentVariablesFillMissingUiSettings()
    {
        var settings = new OpenAiProviderSettings
        {
            ApiKey = "",
            Model = "",
            BaseUri = ""
        };

        var created = OpenAiTranslationOptions.TryCreate(settings, key => key switch
        {
            "OPENAI_API_KEY" => "sk-env",
            "PEAR_TRANSLATOR_OPENAI_MODEL" => "gpt-env",
            "PEAR_TRANSLATOR_OPENAI_BASE_URI" => "https://env.example/v1/",
            _ => null
        }, out var options);

        Assert.True(created);
        Assert.Equal("sk-env", options?.ApiKey);
        Assert.Equal("gpt-env", options?.Model);
        Assert.Equal(new Uri("https://env.example/v1/"), options?.BaseUri);
    }

    private static string? ReadSetting(string key)
    {
        return key switch
        {
            "OPENAI_API_KEY" => " sk-test ",
            _ => null
        };
    }
}
