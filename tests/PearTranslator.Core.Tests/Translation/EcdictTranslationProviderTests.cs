using System.Text;
using PearTranslator.Core.Translation;

namespace PearTranslator.Core.Tests.Translation;

public sealed class EcdictTranslationProviderTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public EcdictTranslationProviderTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task TranslatesExactEntryIgnoringCaseAndWhitespace()
    {
        var dictionaryPath = WriteDictionary("""
            word,phonetic,definition,translation,pos,collins,oxford,tag,bnc,frq,exchange,detail,audio
            quest,,,任务；追寻,,,,,0,0,,,
            start,,,开始；启动,,,,,0,0,,,
            """);
        var provider = new EcdictTranslationProvider(dictionaryPath);

        var translated = await provider.TranslateAsync("  Quest ", CancellationToken.None);

        Assert.Equal("任务；追寻", translated);
        Assert.Equal("本地", provider.ProviderLabel);
    }

    [Fact]
    public async Task TranslatesEntryWithTrailingOcrPunctuation()
    {
        var dictionaryPath = WriteDictionary("""
            word,phonetic,definition,translation,pos,collins,oxford,tag,bnc,frq,exchange,detail,audio
            quest,,,LOCAL_QUEST,,,,,0,0,,,
            """);
        var provider = new EcdictTranslationProvider(dictionaryPath);

        var translated = await provider.TranslateAsync("Quest:", CancellationToken.None);

        Assert.Equal("LOCAL_QUEST:", translated);
    }

    [Fact]
    public async Task TranslatesKnownWordsInsideShortOcrText()
    {
        var dictionaryPath = WriteDictionary("""
            word,phonetic,definition,translation,pos,collins,oxford,tag,bnc,frq,exchange,detail,audio
            open,,,OPEN_ZH,,,,,0,0,,,
            gate,,,GATE_ZH,,,,,0,0,,,
            """);
        var provider = new EcdictTranslationProvider(dictionaryPath);

        var translated = await provider.TranslateAsync("Open gate!", CancellationToken.None);

        Assert.Equal("OPEN_ZH GATE_ZH!", translated);
    }

    [Fact]
    public async Task ThrowsDictionaryMissWhenEntryIsMissing()
    {
        var dictionaryPath = WriteDictionary("""
            word,phonetic,definition,translation,pos,collins,oxford,tag,bnc,frq,exchange,detail,audio
            start,,,开始；启动,,,,,0,0,,,
            """);
        var provider = new EcdictTranslationProvider(dictionaryPath);

        await Assert.ThrowsAsync<LocalDictionaryMissException>(
            () => provider.TranslateAsync("quest", CancellationToken.None));
    }

    [Fact]
    public async Task ThrowsDictionaryMissWhenFileIsMissing()
    {
        var provider = new EcdictTranslationProvider(Path.Combine(_tempDirectory, "missing.csv"));

        await Assert.ThrowsAsync<LocalDictionaryMissException>(
            () => provider.TranslateAsync("quest", CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private string WriteDictionary(string contents)
    {
        var path = Path.Combine(_tempDirectory, "ecdict.csv");
        File.WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }
}
