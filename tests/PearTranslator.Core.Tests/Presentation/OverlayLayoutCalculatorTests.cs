using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Presentation;

namespace PearTranslator.Core.Tests.Presentation;

public sealed class OverlayLayoutCalculatorTests
{
    [Fact]
    public void WindowAndCanvasMatchSelectedRegionSize()
    {
        var region = new FrameRegion(120, 80, 800, 180);

        var layout = OverlayLayoutCalculator.Calculate(region, dpiScaleX: 1.25, dpiScaleY: 1.5);

        Assert.Equal(640, layout.WindowWidth);
        Assert.Equal(120, layout.WindowHeight);
        Assert.Equal(layout.WindowWidth, layout.CanvasWidth);
        Assert.Equal(layout.WindowHeight, layout.CanvasHeight);
    }

    [Fact]
    public void WindowHeightAddsToolbarHeightWithoutShrinkingSelectedRegionCanvas()
    {
        var region = new FrameRegion(120, 80, 800, 180);

        var layout = OverlayLayoutCalculator.Calculate(
            region,
            dpiScaleX: 1.25,
            dpiScaleY: 1.5,
            toolbarHeight: 20);

        Assert.Equal(640, layout.WindowWidth);
        Assert.Equal(140, layout.WindowHeight);
        Assert.Equal(640, layout.CanvasWidth);
        Assert.Equal(120, layout.CanvasHeight);
        Assert.Equal(20, layout.ToolbarHeight);
        Assert.Equal(20, layout.CaptionCanvasTop);
    }

    [Fact]
    public void TextBoundsMapToSameRelativePosition()
    {
        var region = new FrameRegion(120, 80, 800, 180);
        var bounds = new FrameRegion(40, 90, 360, 48);
        var layout = OverlayLayoutCalculator.Calculate(region, dpiScaleX: 2, dpiScaleY: 1.5);

        var placement = OverlayLayoutCalculator.CalculateTextPlacement(
            bounds,
            layout,
            dpiScaleX: 2,
            dpiScaleY: 1.5,
            minimumTextWidth: 80);

        Assert.Equal(20, placement.Left);
        Assert.Equal(60, placement.Top);
    }

    [Fact]
    public void CaptionTopAppliesFontBasedDownwardVisualCompensation()
    {
        var layout = new OverlayLayout(
            WindowWidth: 300,
            WindowHeight: 120,
            CanvasWidth: 300,
            CanvasHeight: 100,
            ToolbarHeight: 20,
            CaptionCanvasTop: 20);
        var placement = new OverlayTextPlacement(Left: 0, Top: 84, TextWidth: 200);

        var top = OverlayLayoutCalculator.CalculateCaptionTop(placement, layout, fontSize: 28);

        Assert.Equal(89.6, top, precision: 3);
    }

    [Fact]
    public void CaptionFontSizeMatchesSourceTextHeightAfterDpiAndLineHeightCompensation()
    {
        var fontSize = OverlayLayoutCalculator.CalculateCaptionFontSize(
            sourceTextHeightPixels: 36,
            dpiScaleY: 1.5);

        Assert.Equal(12.48, fontSize, precision: 2);
    }

    [Fact]
    public void CaptionLinePlacementUsesThatLineTextHeightForFontSize()
    {
        var layout = new OverlayLayout(
            WindowWidth: 400,
            WindowHeight: 160,
            CanvasWidth: 400,
            CanvasHeight: 140,
            ToolbarHeight: 20,
            CaptionCanvasTop: 20);
        var bounds = new FrameRegion(30, 40, 240, 30);

        var placement = OverlayLayoutCalculator.CalculateCaptionLinePlacement(
            bounds,
            layout,
            dpiScaleX: 1,
            dpiScaleY: 1.25,
            minimumTextWidth: 80);

        Assert.Equal(30, placement.Left);
        Assert.Equal(240, placement.TextWidth);
        Assert.Equal(12.48, placement.FontSize, precision: 2);
    }

    [Fact]
    public void WindowOriginFallsBackToVirtualStartWhenWindowIsLargerThanScreen()
    {
        var origin = OverlayLayoutCalculator.ClampWindowOrigin(
            proposedOrigin: 120,
            virtualStart: 0,
            virtualExtent: 900,
            windowExtent: 917.3333333333333);

        Assert.Equal(0, origin);
    }
}
