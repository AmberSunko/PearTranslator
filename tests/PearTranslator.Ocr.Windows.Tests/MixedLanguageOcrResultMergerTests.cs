using PearTranslator.Core.Abstractions;
using PearTranslator.Ocr.Windows;

namespace PearTranslator.Ocr.Windows.Tests;

public sealed class MixedLanguageOcrResultMergerTests
{
    [Fact]
    public void MergeKeepsPrimaryLinesAndAddsNonOverlappingKoreanLines()
    {
        var primary = new OcrResult(
            "Hello",
            TextLines:
            [
                new OcrTextLine("Hello", new FrameRegion(10, 10, 90, 20))
            ]);
        var korean = new OcrResult(
            "안녕하세요",
            TextLines:
            [
                new OcrTextLine("안녕하세요", new FrameRegion(10, 50, 120, 22))
            ]);

        var merged = MixedLanguageOcrResultMerger.Merge(primary, korean);

        Assert.Equal("Hello\n안녕하세요", merged.Text);
        Assert.Collection(
            merged.TextLines!,
            line => Assert.Equal("Hello", line.Text),
            line => Assert.Equal("안녕하세요", line.Text));
    }

    [Fact]
    public void MergeReplacesOverlappingPrimaryLineWhenKoreanLineHasHangul()
    {
        var primary = new OcrResult(
            "oI=?",
            TextLines:
            [
                new OcrTextLine("oI=?", new FrameRegion(10, 10, 110, 24))
            ]);
        var korean = new OcrResult(
            "안녕하세요",
            TextLines:
            [
                new OcrTextLine("안녕하세요", new FrameRegion(12, 11, 108, 23))
            ]);

        var merged = MixedLanguageOcrResultMerger.Merge(primary, korean);

        var line = Assert.Single(merged.TextLines!);
        Assert.Equal("안녕하세요", line.Text);
        Assert.Equal("안녕하세요", merged.Text);
    }
}
