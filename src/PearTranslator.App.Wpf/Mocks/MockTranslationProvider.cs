using PearTranslator.Core.Abstractions;

namespace PearTranslator.App.Wpf.Mocks;

public sealed class MockTranslationProvider : ITranslationProvider, ITranslationProviderMetadata
{
    public string ProviderLabel => "调试";

    public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        var translation = sourceText switch
        {
            "Welcome back" => "欢迎回来",
            "Open the ancient gate" => "打开古老的大门",
            "We must leave before sunrise" => "我们必须在日出前离开",
            _ => $"译文：{sourceText}"
        };

        return Task.FromResult(translation);
    }
}
