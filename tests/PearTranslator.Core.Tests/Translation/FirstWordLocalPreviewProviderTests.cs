using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Translation;

namespace PearTranslator.Core.Tests.Translation;

public sealed class FirstWordLocalPreviewProviderTests
{
    [Fact]
    public async Task CachesPreviewByNormalizedLineText()
    {
        var dictionary = new CountingTranslationProvider(new Dictionary<string, string>
        {
            ["call"] = "呼叫",
        });
        var provider = new FirstWordLocalPreviewProvider(dictionary);
        var lines = new[]
        {
            new OcrTextLine("Call room service", new FrameRegion(10, 10, 200, 20)),
        };

        var first = await provider.BuildPreviewAsync(lines, "Call room service", CancellationToken.None);
        var second = await provider.BuildPreviewAsync(lines, "Call room service", CancellationToken.None);

        Assert.Equal("呼叫", first);
        Assert.Equal("呼叫", second);
        Assert.Equal(1, dictionary.RequestCount);
    }

    [Fact]
    public async Task UsesFirstWordAndFirstCleanDefinitionForEachOcrLine()
    {
        var provider = new FirstWordLocalPreviewProvider(new LocalDictionaryTranslationProvider(
            new Dictionary<string, string>
            {
                ["call"] = "n. 呼叫; 叫喊, 电话",
                ["breakfast"] = "早餐，早饭",
            }));
        var lines = new[]
        {
            new OcrTextLine("Call room service", new FrameRegion(10, 10, 200, 20)),
            new OcrTextLine("Breakfast in bed", new FrameRegion(10, 40, 200, 20)),
        };

        var preview = await provider.BuildPreviewAsync(lines, "Call room service\nBreakfast in bed", CancellationToken.None);

        Assert.Equal("呼叫\n早餐", preview);
        Assert.Equal("local", provider.ProviderLabel);
    }

    [Fact]
    public async Task RemovesDictionaryMetadataBeforeChoosingPreviewDefinition()
    {
        var provider = new FirstWordLocalPreviewProvider(new LocalDictionaryTranslationProvider(
            new Dictionary<string, string>
            {
                ["able"] = "a. \u80fd\u591f\u7684, \u53ef\u4ee5\u7684",
                ["quickly"] = "adv. \u5feb\u901f\u5730; \u8fc5\u901f\u5730",
                ["line"] = "n. [\u7f51\u7edc] \u63a7\u5236\u7ebf\uff1b\u5370\u5df4\u63a7\u5236\u7ebf",
                ["acute"] = "[\u533b] \u6025\u6027\u8111\u79ef\u6c34(\u7ed3\u6838\u6027\u8111\u819c\u708e\u65f6), \u5176\u5b83",
                ["hello"] = "interj. (\u8868\u793a\u95ee\u5019) \u4f60\u597d; \u5582",
                ["suffix"] = "suf. [\u8bed\u6cd5] \u540e\u7f00",
            }));
        var lines = new[]
        {
            new OcrTextLine("Able to move", new FrameRegion(10, 10, 200, 20)),
            new OcrTextLine("Quickly move", new FrameRegion(10, 40, 200, 20)),
            new OcrTextLine("Line of control", new FrameRegion(10, 70, 200, 20)),
            new OcrTextLine("Acute pain", new FrameRegion(10, 100, 200, 20)),
            new OcrTextLine("Hello there", new FrameRegion(10, 130, 200, 20)),
            new OcrTextLine("Suffix marker", new FrameRegion(10, 160, 200, 20)),
        };

        var preview = await provider.BuildPreviewAsync(lines, "fallback", CancellationToken.None);

        Assert.Equal(
            "\u80fd\u591f\u7684\n\u5feb\u901f\u5730\n\u63a7\u5236\u7ebf\n\u6025\u6027\u8111\u79ef\u6c34\n\u4f60\u597d\n\u540e\u7f00",
            preview);
    }

    [Fact]
    public async Task SkipsInflectionOnlyDictionaryDefinitions()
    {
        var provider = new FirstWordLocalPreviewProvider(new LocalDictionaryTranslationProvider(
            new Dictionary<string, string>
            {
                ["is"] = "v. be\u7684\u7b2c\u4e09\u4eba\u79f0\u5355\u6570",
                ["opened"] = "v. open\u7684\u8fc7\u53bb\u5f0f\u548c\u8fc7\u53bb\u5206\u8bcd",
                ["take"] = "\u62ff\u53d6",
            }));
        var lines = new[]
        {
            new OcrTextLine("Is anyone there", new FrameRegion(10, 10, 200, 20)),
            new OcrTextLine("Opened slowly", new FrameRegion(10, 40, 200, 20)),
            new OcrTextLine("Take the key", new FrameRegion(10, 70, 200, 20)),
        };

        var preview = await provider.BuildPreviewAsync(lines, "Is anyone there\nOpened slowly\nTake the key", CancellationToken.None);

        Assert.Equal("\u200B\n\u200B\n\u62ff\u53d6", preview);
    }

    [Fact]
    public async Task ReturnsNullWhenAllDictionaryDefinitionsAreInflectionOnly()
    {
        var provider = new FirstWordLocalPreviewProvider(new LocalDictionaryTranslationProvider(
            new Dictionary<string, string>
            {
                ["is"] = "v. be\u7684\u7b2c\u4e09\u4eba\u79f0\u5355\u6570",
            }));

        var preview = await provider.BuildPreviewAsync(
            [new OcrTextLine("Is anyone there", new FrameRegion(10, 10, 200, 20))],
            "Is anyone there",
            CancellationToken.None);

        Assert.Null(preview);
    }

    [Fact]
    public async Task PreservesLineSlotsWhenOneLineMissesDictionary()
    {
        var provider = new FirstWordLocalPreviewProvider(new LocalDictionaryTranslationProvider(
            new Dictionary<string, string>
            {
                ["open"] = "打开",
                ["take"] = "拿取",
            }));
        var lines = new[]
        {
            new OcrTextLine("Open the door", new FrameRegion(10, 10, 200, 20)),
            new OcrTextLine("Unknown word", new FrameRegion(10, 40, 200, 20)),
            new OcrTextLine("Take the key", new FrameRegion(10, 70, 200, 20)),
        };

        var preview = await provider.BuildPreviewAsync(lines, "Open the door\nUnknown word\nTake the key", CancellationToken.None);

        Assert.Equal("打开\n\u200B\n拿取", preview);
    }

    [Fact]
    public async Task ReturnsNullWhenNoLineCanBePreviewed()
    {
        var provider = new FirstWordLocalPreviewProvider(new LocalDictionaryTranslationProvider(
            new Dictionary<string, string>
            {
                ["open"] = "打开",
            }));

        var preview = await provider.BuildPreviewAsync(
            [new OcrTextLine("Unknown word", new FrameRegion(10, 10, 200, 20))],
            "Unknown word",
            CancellationToken.None);

            Assert.Null(preview);
    }

    private sealed class CountingTranslationProvider : ITranslationProvider
    {
        private readonly IReadOnlyDictionary<string, string> _entries;

        public CountingTranslationProvider(IReadOnlyDictionary<string, string> entries)
        {
            _entries = entries;
        }

        public int RequestCount { get; private set; }

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            RequestCount++;
            return _entries.TryGetValue(sourceText.ToLowerInvariant(), out var translated)
                ? Task.FromResult(translated)
                : throw new LocalDictionaryMissException(sourceText);
        }
    }
}
