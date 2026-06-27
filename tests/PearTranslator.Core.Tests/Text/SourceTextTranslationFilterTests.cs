using PearTranslator.Core.Text;
using PearTranslator.Core.Configuration;

namespace PearTranslator.Core.Tests.Text;

public sealed class SourceTextTranslationFilterTests
{
    [Fact]
    public void ShouldTranslateReturnsTrueForEnglishText()
    {
        Assert.True(SourceTextTranslationFilter.ShouldTranslate("Open the ancient door."));
    }

    [Fact]
    public void ShouldTranslateReturnsTrueForMixedChineseAndEnglishText()
    {
        Assert.True(SourceTextTranslationFilter.ShouldTranslate("按 E Open"));
    }

    [Fact]
    public void ShouldTranslateReturnsFalseForChineseText()
    {
        Assert.False(SourceTextTranslationFilter.ShouldTranslate("你好，世界"));
    }

    [Fact]
    public void ShouldTranslateReturnsTrueForChineseTextWhenTargetLanguageIsEnglish()
    {
        Assert.True(SourceTextTranslationFilter.ShouldTranslate("你好，世界", TargetLanguage.English));
    }

    [Fact]
    public void ShouldTranslateReturnsFalseForEnglishTextWhenTargetLanguageIsEnglish()
    {
        Assert.False(SourceTextTranslationFilter.ShouldTranslate("Open the ancient door.", TargetLanguage.English));
    }

    [Fact]
    public void ShouldTranslateReturnsTrueForJapaneseTextWhenTargetLanguageIsChinese()
    {
        Assert.True(SourceTextTranslationFilter.ShouldTranslate("古い門がゆっくり開いた。"));
    }

    [Fact]
    public void ShouldTranslateReturnsTrueForKoreanTextWhenTargetLanguageIsChinese()
    {
        Assert.True(SourceTextTranslationFilter.ShouldTranslate("차가운 바람이 복도를 지나갔다."));
    }

    [Fact]
    public void ShouldTranslateReturnsFalseForNumbersAndPunctuationOnly()
    {
        Assert.False(SourceTextTranslationFilter.ShouldTranslate("100 / 200"));
    }

    [Theory]
    [InlineData("text")]
    [InlineData("txt")]
    [InlineData("plaintext")]
    public void ShouldTranslateReturnsFalseForStandaloneCodeBlockLanguageLabels(string text)
    {
        Assert.False(SourceTextTranslationFilter.ShouldTranslate(text));
    }
}
