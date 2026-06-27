using PearTranslator.App.Wpf;

namespace PearTranslator.App.Wpf.Tests;

public sealed class DwmGlassFrameTests
{
    [Fact]
    public void TopGlassHeightCoversTitleBarAndContentTopCorners()
    {
        var height = WindowChromeMetrics.CalculateTopGlassHeight(
            titleBarHeight: 46,
            contentTopLeftRadius: 16,
            contentTopRightRadius: 16);

        Assert.Equal(62, height);
    }

    [Fact]
    public void TopGlassHeightUsesLargestContentTopCorner()
    {
        var height = WindowChromeMetrics.CalculateTopGlassHeight(
            titleBarHeight: 46,
            contentTopLeftRadius: 8,
            contentTopRightRadius: 20);

        Assert.Equal(66, height);
    }

    [Fact]
    public void ConvertsTopAndBottomChromeHeightsToPhysicalPixels()
    {
        var frame = DwmGlassFrame.FromDeviceIndependentPixels(
            topDip: 46,
            bottomDip: 72,
            dpiScaleY: 1.25);

        Assert.Equal(58, frame.TopPixels);
        Assert.Equal(90, frame.BottomPixels);
    }

    [Fact]
    public void IgnoresInvalidGlassFrameSizes()
    {
        var frame = DwmGlassFrame.FromDeviceIndependentPixels(
            topDip: -1,
            bottomDip: 72,
            dpiScaleY: 0);

        Assert.Equal(0, frame.TopPixels);
        Assert.Equal(0, frame.BottomPixels);
    }

    [Fact]
    public void BottomGlassHeightCoversFooterMarginsAndMinimumBand()
    {
        var height = WindowChromeMetrics.CalculateBottomGlassHeight(
            actualFooterHeight: 36,
            marginTop: 0,
            marginBottom: 18,
            minimumHeight: 72);

        Assert.Equal(72, height);
    }

    [Fact]
    public void BottomGlassHeightExpandsForTallFooterContent()
    {
        var height = WindowChromeMetrics.CalculateBottomGlassHeight(
            actualFooterHeight: 80,
            marginTop: 4,
            marginBottom: 18,
            minimumHeight: 72);

        Assert.Equal(102, height);
    }

    [Fact]
    public void BottomGlassHeightCoversContentBottomCorners()
    {
        var height = WindowChromeMetrics.CalculateBottomGlassHeight(
            actualFooterHeight: 36,
            marginTop: 0,
            marginBottom: 18,
            minimumHeight: 72,
            contentBottomMargin: 18,
            contentBottomLeftRadius: 16,
            contentBottomRightRadius: 16);

        Assert.Equal(88, height);
    }

    [Fact]
    public void BottomGlassHeightUsesLargestContentBottomCorner()
    {
        var height = WindowChromeMetrics.CalculateBottomGlassHeight(
            actualFooterHeight: 36,
            marginTop: 0,
            marginBottom: 18,
            minimumHeight: 72,
            contentBottomMargin: 18,
            contentBottomLeftRadius: 8,
            contentBottomRightRadius: 20);

        Assert.Equal(92, height);
    }
}
