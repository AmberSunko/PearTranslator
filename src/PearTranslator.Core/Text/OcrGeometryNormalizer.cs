using PearTranslator.Core.Abstractions;

namespace PearTranslator.Core.Text;

public static class OcrGeometryNormalizer
{
    public static NormalizedOcrGeometry Normalize(
        FrameRegion captureRegion,
        int imageWidthPixels,
        int imageHeightPixels,
        FrameRegion? imageTextBoundsPixels,
        double? imageTextHeightPixels,
        IReadOnlyList<OcrTextLine>? imageTextLines = null)
    {
        if (imageWidthPixels <= 0 || imageHeightPixels <= 0)
        {
            return new NormalizedOcrGeometry(
                imageTextHeightPixels,
                imageTextBoundsPixels,
                imageTextLines ?? []);
        }

        var scaleX = captureRegion.Width / (double)imageWidthPixels;
        var scaleY = captureRegion.Height / (double)imageHeightPixels;
        FrameRegion? textBounds = imageTextBoundsPixels is { } bounds
            ? ScaleBounds(bounds, scaleX, scaleY)
            : null;
        double? textHeight = imageTextHeightPixels is { } height
            ? height * scaleY
            : null;
        var textLines = ScaleLines(imageTextLines, scaleX, scaleY);

        return new NormalizedOcrGeometry(textHeight, textBounds, textLines);
    }

    private static FrameRegion ScaleBounds(FrameRegion bounds, double scaleX, double scaleY)
    {
        return new FrameRegion(
            (int)Math.Round(bounds.X * scaleX),
            (int)Math.Round(bounds.Y * scaleY),
            Math.Max(1, (int)Math.Round(bounds.Width * scaleX)),
            Math.Max(1, (int)Math.Round(bounds.Height * scaleY)));
    }

    private static IReadOnlyList<OcrTextLine> ScaleLines(
        IReadOnlyList<OcrTextLine>? lines,
        double scaleX,
        double scaleY)
    {
        if (lines is null || lines.Count == 0)
        {
            return [];
        }

        return lines
            .Select(line => line.BoundsPixels is { } bounds
                ? line with { BoundsPixels = ScaleBounds(bounds, scaleX, scaleY) }
                : line)
            .ToArray();
    }
}

public readonly record struct NormalizedOcrGeometry(
    double? EstimatedTextHeightPixels,
    FrameRegion? TextBoundsPixels,
    IReadOnlyList<OcrTextLine> TextLines);
