using PearTranslator.Core.Text;
using PearTranslator.Core.Configuration;

namespace PearTranslator.Core.Tests.Text;

public sealed class SourceTextTranslationExtractorTests
{
    [Fact]
    public void ExtractTranslatableTextRemovesChineseCharactersFromMixedSource()
    {
        var extracted = SourceTextTranslationExtractor.ExtractTranslatableText("按 E Open the ancient door");

        Assert.Equal("E Open the ancient door", extracted);
    }

    [Fact]
    public void ExtractTranslatableTextRemovesChinesePunctuationAroundRemovedChineseText()
    {
        var extracted = SourceTextTranslationExtractor.ExtractTranslatableText("提示：Open the map。");

        Assert.Equal("Open the map", extracted);
    }

    [Fact]
    public void ExtractTranslatableTextReturnsEmptyForChineseOnlySource()
    {
        var extracted = SourceTextTranslationExtractor.ExtractTranslatableText("你好，世界");

        Assert.Equal(string.Empty, extracted);
    }

    [Fact]
    public void ExtractTranslatableTextKeepsChineseWhenTargetLanguageIsEnglish()
    {
        var extracted = SourceTextTranslationExtractor.ExtractTranslatableText(
            "你好，世界",
            TargetLanguage.English);

        Assert.Equal("你好，世界", extracted);
    }

    [Fact]
    public void ExtractTranslatableTextRemovesEnglishWhenTargetLanguageIsEnglish()
    {
        var extracted = SourceTextTranslationExtractor.ExtractTranslatableText(
            "按 E Open the door",
            TargetLanguage.English);

        Assert.Equal("按", extracted);
    }

    [Fact]
    public void ExtractTranslatableTextKeepsJapaneseWhenTargetLanguageIsChinese()
    {
        var extracted = SourceTextTranslationExtractor.ExtractTranslatableText("古い門がゆっくり開いた。");

        Assert.Equal("古い門がゆっくり開いた。", extracted);
    }

    [Fact]
    public void ExtractTranslatableTextKeepsKoreanWhenTargetLanguageIsChinese()
    {
        var extracted = SourceTextTranslationExtractor.ExtractTranslatableText("차가운 바람이 복도를 지나갔다.");

        Assert.Equal("차가운 바람이 복도를 지나갔다.", extracted);
    }
}
