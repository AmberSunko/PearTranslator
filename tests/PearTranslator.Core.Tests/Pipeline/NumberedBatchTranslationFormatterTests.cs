using PearTranslator.Core.Configuration;
using PearTranslator.Core.Pipeline;

namespace PearTranslator.Core.Tests.Pipeline;

public sealed class NumberedBatchTranslationFormatterTests
{
    [Fact]
    public void BuildPromptIncludesNumberedLinePayload()
    {
        var prompt = NumberedBatchTranslationFormatter.BuildPrompt(
            ["The ancient gate opened slowly.", "A cold wind moved through the hall."]);

        Assert.Contains("\"id\":1", prompt);
        Assert.Contains("\"text\":\"The ancient gate opened slowly.\"", prompt);
        Assert.Contains("\"id\":2", prompt);
        Assert.Contains("Return only a JSON array", prompt);
    }

    [Fact]
    public void BuildPromptUsesEnglishTargetLanguageInstruction()
    {
        var prompt = NumberedBatchTranslationFormatter.BuildPrompt(
            ["你好，世界。"],
            TargetLanguage.English);

        Assert.Contains("natural English", prompt);
        Assert.Contains("non-English", prompt);
    }

    [Fact]
    public void TryParseReturnsTranslationsInSourceOrder()
    {
        var translated = """
            [{"id":2,"text":"一阵冷风穿过大厅。"},{"id":"1","text":"古老的大门缓缓打开。"}]
            """;

        var parsed = NumberedBatchTranslationFormatter.TryParse(
            translated,
            expectedLineCount: 2,
            out var translatedLines);

        Assert.True(parsed);
        Assert.Equal(["古老的大门缓缓打开。", "一阵冷风穿过大厅。"], translatedLines);
    }

    [Fact]
    public void TryParseExtractsJsonArrayFromWrappedText()
    {
        var parsed = NumberedBatchTranslationFormatter.TryParse(
            "```json\n[{\"id\":1,\"text\":\"你好\"}]\n```",
            expectedLineCount: 1,
            out var translatedLines);

        Assert.True(parsed);
        Assert.Equal(["你好"], translatedLines);
    }

    [Theory]
    [InlineData("[{\"id\":1,\"text\":\"第一行\"},{\"id\":1,\"text\":\"重复\"}]")]
    [InlineData("[{\"id\":2,\"text\":\"越界\"}]")]
    [InlineData("[{\"id\":1,\"text\":\"\"}]")]
    [InlineData("{\"id\":1,\"text\":\"不是数组\"}")]
    public void TryParseRejectsMalformedOrIncompleteResponses(string translated)
    {
        var parsed = NumberedBatchTranslationFormatter.TryParse(
            translated,
            expectedLineCount: 1,
            out var translatedLines);

        Assert.False(parsed);
        Assert.Empty(translatedLines);
    }
}
