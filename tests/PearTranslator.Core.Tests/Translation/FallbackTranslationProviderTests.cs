using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Translation;

namespace PearTranslator.Core.Tests.Translation;

public sealed class FallbackTranslationProviderTests
{
    [Fact]
    public async Task UsesFirstProviderThatReturnsTranslationAndExposesItsLabel()
    {
        var local = new LocalDictionaryTranslationProvider(new Dictionary<string, string>
        {
            ["quest"] = "任务"
        });
        var remote = new FakeProvider("remote", "远程翻译");
        var provider = new FallbackTranslationProvider([local, remote]);

        var translated = await provider.TranslateAsync("Quest", CancellationToken.None);

        Assert.Equal("任务", translated);
        Assert.Equal("本地", provider.ProviderLabel);
    }

    [Fact]
    public async Task FallsBackWhenLocalDictionaryMisses()
    {
        var local = new LocalDictionaryTranslationProvider(new Dictionary<string, string>());
        var remote = new FakeProvider("DeepL", "你好");
        var provider = new FallbackTranslationProvider([local, remote]);

        var translated = await provider.TranslateAsync("Hello", CancellationToken.None);

        Assert.Equal("你好", translated);
        Assert.Equal("DeepL", provider.ProviderLabel);
    }

    [Fact]
    public async Task FallsBackWhenProviderThrows()
    {
        var remote = new ThrowingProvider();
        var preview = new FakeProvider("识别", "Hello");
        var provider = new FallbackTranslationProvider([remote, preview]);

        var translated = await provider.TranslateAsync("Hello", CancellationToken.None);

        Assert.Equal("Hello", translated);
        Assert.Equal("识别", provider.ProviderLabel);
    }

    private sealed class FakeProvider(string label, string translation) : ITranslationProvider, ITranslationProviderMetadata
    {
        public string ProviderLabel => label;

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            return Task.FromResult(translation);
        }
    }

    private sealed class ThrowingProvider : ITranslationProvider, ITranslationProviderMetadata
    {
        public string ProviderLabel => "remote";

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("bad api key");
        }
    }
}
