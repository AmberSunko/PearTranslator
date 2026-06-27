using PearTranslator.Core.Text;
using PearTranslator.Core.Translation;

namespace PearTranslator.Core.Tests.Text;

public sealed class MultilineTextFlowTests
{
    [Fact]
    public void TextNormalizerCanPreserveLineBreaks()
    {
        var normalized = TextNormalizer.NormalizePreservingLineBreaks("  Open   the door\r\n\r\n Take   the key  ");

        Assert.Equal("Open the door\nTake the key", normalized);
    }

    [Fact]
    public void SourceExtractorPreservesLineBreaks()
    {
        var extracted = SourceTextTranslationExtractor.ExtractTranslatableText("Open   the door\r\nTake the key");

        Assert.Equal("Open the door\nTake the key", extracted);
    }

    [Fact]
    public void TranslationDisplayNormalizerPreservesLineBreaks()
    {
        var normalized = TranslationTextNormalizer.NormalizeForDisplay("Line   one\r\nLine   two");

        Assert.Equal("Line one\nLine two", normalized);
    }

    [Fact]
    public void TranslationCachePreservesLineBreaksInCachedTranslation()
    {
        var cache = new TranslationCache();

        cache.Store("Open the door\nTake the key", "Line   one\r\nLine   two");

        Assert.True(cache.TryGet("Open the door\r\nTake   the key", out var translation));
        Assert.Equal("Line one\nLine two", translation);
    }
}
