using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Text;

namespace PearTranslator.Core.Tests.Text;

public sealed class OcrLineTextExtractorTests
{
    [Fact]
    public void GetTranslatableBoundedLinesFiltersUnboundedAndNonEnglishLines()
    {
        var lines = new[]
        {
            new OcrTextLine("Maybe we can call room service", new FrameRegion(10, 10, 200, 20)),
            new OcrTextLine("中文不用翻译", new FrameRegion(10, 40, 200, 20)),
            new OcrTextLine("The ancient gate opened slowly")
        };

        var translatable = OcrLineTextExtractor.GetTranslatableBoundedLines(lines);

        var line = Assert.Single(translatable);
        Assert.Equal("Maybe we can call room service", line.Text);
    }

    [Fact]
    public void GetTranslatableBoundedLineTextsExtractsCleanText()
    {
        var lines = new[]
        {
            new OcrTextLine("Maybe we can call room service", new FrameRegion(10, 10, 200, 20)),
            new OcrTextLine("A cold wind moved through the hall.", new FrameRegion(10, 40, 200, 20))
        };

        var texts = OcrLineTextExtractor.GetTranslatableBoundedLineTexts(lines);

        Assert.Equal(
            [
                "Maybe we can call room service",
                "A cold wind moved through the hall."
            ],
            texts);
    }

    [Fact]
    public void GetTranslatableFallbackLineTextsPreservesEnglishLineBreaks()
    {
        var texts = OcrLineTextExtractor.GetTranslatableFallbackLineTexts(
            "Maybe we can call room service\r\n中文不用翻译\nThe ancient gate opened slowly.");

        Assert.Equal(
            [
                "Maybe we can call room service",
                "The ancient gate opened slowly."
            ],
            texts);
    }

    [Fact]
    public void GetTranslatableBoundedLineTextsExtractsChineseWhenTargetLanguageIsEnglish()
    {
        var lines = new[]
        {
            new OcrTextLine("你好，世界", new FrameRegion(10, 10, 200, 20)),
            new OcrTextLine("Open the door", new FrameRegion(10, 40, 200, 20))
        };

        var texts = OcrLineTextExtractor.GetTranslatableBoundedLineTexts(
            lines,
            TargetLanguage.English);

        Assert.Equal(["你好，世界"], texts);
    }
}
