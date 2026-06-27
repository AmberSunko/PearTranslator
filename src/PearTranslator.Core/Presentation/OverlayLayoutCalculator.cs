using PearTranslator.Core.Abstractions;

namespace PearTranslator.Core.Presentation;

public static class OverlayLayoutCalculator
{
    public const double DefaultCaptionFontSize = 28;
    public const double MinCaptionFontSize = 8;
    public const double MaxCaptionFontSize = 96;
    private const double SourceTextHeightToFontSizeRatio = 0.52;

    public static OverlayLayout Calculate(
        FrameRegion region,
        double dpiScaleX,
        double dpiScaleY,
        double toolbarHeight = 0)
    {
        var width = Math.Max(1, region.Width / Math.Max(0.001, dpiScaleX));
        var canvasHeight = Math.Max(1, region.Height / Math.Max(0.001, dpiScaleY));
        var normalizedToolbarHeight = Math.Max(0, toolbarHeight);
        var windowHeight = canvasHeight + normalizedToolbarHeight;

        return new OverlayLayout(
            width,
            windowHeight,
            width,
            canvasHeight,
            normalizedToolbarHeight,
            normalizedToolbarHeight);
    }

    public static OverlayTextPlacement CalculateTextPlacement(
        FrameRegion textBounds,
        OverlayLayout layout,
        double dpiScaleX,
        double dpiScaleY,
        double minimumTextWidth)
    {
        var left = Math.Clamp(textBounds.X / Math.Max(0.001, dpiScaleX), 0, layout.CanvasWidth);
        var top = Math.Clamp(textBounds.Y / Math.Max(0.001, dpiScaleY), 0, layout.CanvasHeight);
        var textWidth = Math.Max(minimumTextWidth, textBounds.Width / Math.Max(0.001, dpiScaleX));
        var remainingWidth = Math.Max(minimumTextWidth, layout.CanvasWidth - left);

        return new OverlayTextPlacement(left, top, Math.Min(remainingWidth, textWidth));
    }

    public static double CalculateCaptionTop(
        OverlayTextPlacement placement,
        OverlayLayout layout,
        double fontSize)
    {
        var visualCompensation = Math.Clamp(fontSize * 0.2, 2, 8);
        return Math.Clamp(placement.Top + visualCompensation, 0, layout.CanvasHeight);
    }

    public static double CalculateCaptionFontSize(
        double? sourceTextHeightPixels,
        double dpiScaleY)
    {
        if (sourceTextHeightPixels is not > 0)
        {
            return DefaultCaptionFontSize;
        }

        var sourceHeightDip = sourceTextHeightPixels.Value / Math.Max(0.001, dpiScaleY);
        return Math.Clamp(
            sourceHeightDip * SourceTextHeightToFontSizeRatio,
            MinCaptionFontSize,
            MaxCaptionFontSize);
    }

    public static OverlayCaptionLinePlacement CalculateCaptionLinePlacement(
        FrameRegion textBounds,
        OverlayLayout layout,
        double dpiScaleX,
        double dpiScaleY,
        double minimumTextWidth)
    {
        var placement = CalculateTextPlacement(
            textBounds,
            layout,
            dpiScaleX,
            dpiScaleY,
            minimumTextWidth);
        var fontSize = CalculateCaptionFontSize(textBounds.Height, dpiScaleY);

        return new OverlayCaptionLinePlacement(
            placement.Left,
            CalculateCaptionTop(placement, layout, fontSize),
            placement.TextWidth,
            fontSize);
    }

    public static double ClampWindowOrigin(
        double proposedOrigin,
        double virtualStart,
        double virtualExtent,
        double windowExtent)
    {
        var maxOrigin = virtualStart + Math.Max(0, virtualExtent) - Math.Max(0, windowExtent);
        if (maxOrigin < virtualStart)
        {
            return virtualStart;
        }

        return Math.Clamp(proposedOrigin, virtualStart, maxOrigin);
    }
}

public readonly record struct OverlayLayout(
    double WindowWidth,
    double WindowHeight,
    double CanvasWidth,
    double CanvasHeight,
    double ToolbarHeight,
    double CaptionCanvasTop);

public readonly record struct OverlayTextPlacement(
    double Left,
    double Top,
    double TextWidth);

public readonly record struct OverlayCaptionLinePlacement(
    double Left,
    double Top,
    double TextWidth,
    double FontSize);
