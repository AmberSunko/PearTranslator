using PearTranslator.Core.Text;

namespace PearTranslator.Core.Tests.Text;

public sealed class TranslationTextNormalizerTests
{
    [Fact]
    public void NormalizeForDisplayRemovesSpacesBetweenCjkCharacters()
    {
        var normalized = TranslationTextNormalizer.NormalizeForDisplay("这 是 一 段 翻 译");

        Assert.Equal("这是一段翻译", normalized);
    }

    [Fact]
    public void NormalizeForDisplayRemovesSpacesAroundCjkPunctuation()
    {
        var normalized = TranslationTextNormalizer.NormalizeForDisplay("快 走 ， 守 卫 回 来 了 ！");

        Assert.Equal("快走，守卫回来了！", normalized);
    }

    [Fact]
    public void NormalizeForDisplayKeepsMeaningfulAsciiSpaces()
    {
        var normalized = TranslationTextNormalizer.NormalizeForDisplay("获得 HP 10 点");

        Assert.Equal("获得 HP 10 点", normalized);
    }
}
