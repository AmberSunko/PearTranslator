using PearTranslator.Core.Configuration;

namespace PearTranslator.Core.Tests.Configuration;

public sealed class TranslatorOptionsTests
{
    [Fact]
    public void DefaultsToSingleStableRepeatForRealtimeGames()
    {
        var options = new TranslatorOptions();

        Assert.Equal(1, options.RequiredStableRepeats);
        Assert.True(options.LocalPreviewEnabled);
    }
}
