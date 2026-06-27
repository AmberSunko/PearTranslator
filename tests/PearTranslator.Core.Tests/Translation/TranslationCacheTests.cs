using PearTranslator.Core.Translation;

namespace PearTranslator.Core.Tests.Translation;

public sealed class TranslationCacheTests
{
    [Fact]
    public void ReturnsCachedTranslationForNormalizedSource()
    {
        var cache = new TranslationCache();

        cache.Store("Hello   world", "你好，世界");

        Assert.True(cache.TryGet("Hello world", out var translation));
        Assert.Equal("你好，世界", translation);
    }

    [Fact]
    public void DoesNotStoreBlankSourceOrTranslation()
    {
        var cache = new TranslationCache();

        cache.Store("", "你好");
        cache.Store("Hello", "");

        Assert.False(cache.TryGet("", out _));
        Assert.False(cache.TryGet("Hello", out _));
    }
}
