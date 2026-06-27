using PearTranslator.Core.Presentation;

namespace PearTranslator.Core.Tests.Presentation;

public sealed class OverlayBackgroundModeTests
{
    [Fact]
    public void CyclesThroughFullTransparentAndSourceRegions()
    {
        var transparent = OverlayBackgroundModeCycler.Next(OverlayBackgroundMode.Full);
        var sourceRegions = OverlayBackgroundModeCycler.Next(transparent);
        var full = OverlayBackgroundModeCycler.Next(sourceRegions);

        Assert.Equal(OverlayBackgroundMode.Transparent, transparent);
        Assert.Equal(OverlayBackgroundMode.SourceRegions, sourceRegions);
        Assert.Equal(OverlayBackgroundMode.Full, full);
    }
}
