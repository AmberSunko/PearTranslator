using PearTranslator.Core.Abstractions;
using PearTranslator.Ocr.Windows;

namespace PearTranslator.Ocr.Windows.Tests;

public sealed class PixelOcrResultSelectorTests
{
    [Fact]
    public void SelectBestPrefersReadableSentenceOverShortFragment()
    {
        var fragment = new OcrResult("That s");
        var sentence = new OcrResult("That's a really fine bed. Can't wait to get");

        var selected = PixelOcrResultSelector.SelectBest([fragment, sentence]);

        Assert.Same(sentence, selected);
    }

    [Fact]
    public void SelectBestPenalizesSymbolHeavyNoise()
    {
        var noise = new OcrResult("||| ### 111");
        var text = new OcrResult("full of beautiful crumbs");

        var selected = PixelOcrResultSelector.SelectBest([noise, text]);

        Assert.Same(text, selected);
    }
}
