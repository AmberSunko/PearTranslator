using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Pipeline;

namespace PearTranslator.Core.Tests.Pipeline;

public sealed class TranslationConnectionProbeTests
{
    [Fact]
    public async Task ProbeUsesShortWarmupTextAndReportsProviderLabel()
    {
        var provider = new FakeProvider("deepseek-chat", "ok");

        var result = await TranslationConnectionProbe.ProbeAsync(provider, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("deepseek-chat", result.ProviderLabel);
        Assert.Equal("Hello", provider.LastSourceText);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.Latency >= TimeSpan.Zero);
    }

    [Fact]
    public async Task ProbeReturnsFailureResultWhenProviderThrows()
    {
        var provider = new ThrowingProvider();

        var result = await TranslationConnectionProbe.ProbeAsync(provider, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("fake-failing", result.ProviderLabel);
        Assert.Contains("unavailable", result.ErrorMessage);
        Assert.True(result.Latency >= TimeSpan.Zero);
    }

    private sealed class FakeProvider(string label, string translation) : ITranslationProvider, ITranslationProviderMetadata
    {
        public string ProviderLabel => label;

        public string? LastSourceText { get; private set; }

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            LastSourceText = sourceText;
            return Task.FromResult(translation);
        }
    }

    private sealed class ThrowingProvider : ITranslationProvider, ITranslationProviderMetadata
    {
        public string ProviderLabel => "fake-failing";

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("translation unavailable");
        }
    }
}
