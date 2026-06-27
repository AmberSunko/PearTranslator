using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Presentation;

namespace PearTranslator.Core.Tests.Presentation;

public sealed class OverlayRegionPolicyTests
{
    [Fact]
    public void OverlappingRegionsBlockRealtimeOverlay()
    {
        var realtime = new FrameRegion(10, 10, 100, 60);
        var oneShot = new FrameRegion(80, 40, 120, 80);

        Assert.True(OverlayRegionPolicy.Overlaps(realtime, oneShot));
    }

    [Fact]
    public void EdgeTouchingRegionsCanCoexist()
    {
        var realtime = new FrameRegion(10, 10, 100, 60);
        var oneShot = new FrameRegion(110, 10, 120, 60);

        Assert.False(OverlayRegionPolicy.Overlaps(realtime, oneShot));
    }
}
