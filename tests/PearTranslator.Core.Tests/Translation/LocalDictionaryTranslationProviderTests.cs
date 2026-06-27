using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Translation;

namespace PearTranslator.Core.Tests.Translation;

public sealed class LocalDictionaryTranslationProviderTests
{
    [Fact]
    public async Task TranslatesExactDictionaryEntryIgnoringCaseAndWhitespace()
    {
        var provider = new LocalDictionaryTranslationProvider(new Dictionary<string, string>
        {
            ["quest"] = "任务"
        });

        var translated = await provider.TranslateAsync("  Quest ", CancellationToken.None);

        Assert.Equal("任务", translated);
        Assert.Equal("本地", provider.ProviderLabel);
    }

    [Fact]
    public async Task TranslatesSimpleTemplateEntry()
    {
        var provider = new LocalDictionaryTranslationProvider(new Dictionary<string, string>
        {
            ["you obtained {item}"] = "你获得了{item}"
        });

        var translated = await provider.TranslateAsync("You obtained Ancient Key", CancellationToken.None);

        Assert.Equal("你获得了Ancient Key", translated);
    }

    [Fact]
    public async Task TranslatesEntryWithTrailingOcrPunctuation()
    {
        var provider = new LocalDictionaryTranslationProvider(new Dictionary<string, string>
        {
            ["quest"] = "LOCAL_QUEST"
        });

        var translated = await provider.TranslateAsync("Quest:", CancellationToken.None);

        Assert.Equal("LOCAL_QUEST:", translated);
    }

    [Fact]
    public async Task TranslatesKnownWordsInsideShortOcrText()
    {
        var provider = new LocalDictionaryTranslationProvider(new Dictionary<string, string>
        {
            ["open"] = "OPEN_ZH",
            ["gate"] = "GATE_ZH"
        });

        var translated = await provider.TranslateAsync("Open gate!", CancellationToken.None);

        Assert.Equal("OPEN_ZH GATE_ZH!", translated);
    }

    [Fact]
    public async Task OmitsEmptyDictionaryWordsInsideShortOcrText()
    {
        var provider = new LocalDictionaryTranslationProvider(new Dictionary<string, string>
        {
            ["open"] = "OPEN_ZH",
            ["the"] = string.Empty,
            ["gate"] = "GATE_ZH"
        });

        var translated = await provider.TranslateAsync("Open the gate!", CancellationToken.None);

        Assert.Equal("OPEN_ZH GATE_ZH!", translated);
    }

    [Fact]
    public async Task ThrowsWhenNoLocalEntryMatches()
    {
        var provider = new LocalDictionaryTranslationProvider(new Dictionary<string, string>
        {
            ["quest"] = "任务"
        });

        await Assert.ThrowsAsync<LocalDictionaryMissException>(
            () => provider.TranslateAsync("Hello there", CancellationToken.None));
    }
}
