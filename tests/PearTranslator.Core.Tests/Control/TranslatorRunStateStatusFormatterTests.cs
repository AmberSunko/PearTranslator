using PearTranslator.Core.Control;

namespace PearTranslator.Core.Tests.Control;

public sealed class TranslatorRunStateStatusFormatterTests
{
    [Theory]
    [InlineData(TranslatorRunState.Running, "正在运行")]
    [InlineData(TranslatorRunState.Paused, "已暂停")]
    [InlineData(TranslatorRunState.Dismissed, "已隐藏当前字幕")]
    public void FormatReturnsCompactChineseStatusWithoutFullStop(
        TranslatorRunState state,
        string expected)
    {
        Assert.Equal(expected, TranslatorRunStateStatusFormatter.Format(state));
    }
}
