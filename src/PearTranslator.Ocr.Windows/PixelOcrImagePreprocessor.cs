using SkiaSharp;

namespace PearTranslator.Ocr.Windows;

public static class PixelOcrImagePreprocessor
{
    private const int MaxOcrLongSidePixels = 900;
    private const int MaxOcrTotalPixels = 450_000;

    public static PixelOcrCandidateSet CreateCandidates(SKBitmap source)
    {
        using var resized = ResizeToOcrBudget(source);
        var candidateSource = resized ?? source;
        var grayscale = CreateGrayscale(candidateSource);
        var name = resized is null ? "gray-1x" : "gray-fit";

        return new PixelOcrCandidateSet(
            [
                new PixelOcrImageCandidate(name, grayscale)
            ]);
    }

    private static SKBitmap? ResizeToOcrBudget(SKBitmap source)
    {
        var sourcePixels = source.Width * source.Height;
        var longSide = Math.Max(source.Width, source.Height);
        if (longSide <= MaxOcrLongSidePixels && sourcePixels <= MaxOcrTotalPixels)
        {
            return null;
        }

        var longSideScale = MaxOcrLongSidePixels / (double)longSide;
        var totalPixelsScale = Math.Sqrt(MaxOcrTotalPixels / (double)sourcePixels);
        var scale = Math.Min(longSideScale, totalPixelsScale);
        var targetWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
        return ResizeNearest(source, targetWidth, targetHeight);
    }

    private static SKBitmap ResizeNearest(SKBitmap source, int targetWidth, int targetHeight)
    {
        var target = new SKBitmap(targetWidth, targetHeight, source.ColorType, SKAlphaType.Premul);
        var sourcePixels = source.Pixels;
        var targetPixels = new SKColor[targetWidth * targetHeight];

        for (var y = 0; y < target.Height; y++)
        {
            var sourceY = Math.Min(source.Height - 1, (int)(y / (double)target.Height * source.Height));
            var sourceRowStart = sourceY * source.Width;
            var targetRowStart = y * target.Width;
            for (var x = 0; x < target.Width; x++)
            {
                var sourceX = Math.Min(source.Width - 1, (int)(x / (double)target.Width * source.Width));
                targetPixels[targetRowStart + x] = sourcePixels[sourceRowStart + sourceX];
            }
        }

        target.Pixels = targetPixels;
        return target;
    }

    private static SKBitmap CreateGrayscale(SKBitmap source)
    {
        var target = new SKBitmap(source.Width, source.Height, source.ColorType, SKAlphaType.Premul);
        var sourcePixels = source.Pixels;
        var targetPixels = new SKColor[sourcePixels.Length];
        for (var index = 0; index < sourcePixels.Length; index++)
        {
            var luminance = CalculateLuminance(sourcePixels[index]);
            targetPixels[index] = new SKColor(luminance, luminance, luminance);
        }

        target.Pixels = targetPixels;
        return target;
    }

    private static byte CalculateLuminance(SKColor color)
    {
        return (byte)Math.Clamp(
            (int)Math.Round((0.299 * color.Red) + (0.587 * color.Green) + (0.114 * color.Blue)),
            0,
            255);
    }
}

public sealed class PixelOcrCandidateSet : IDisposable
{
    public PixelOcrCandidateSet(IReadOnlyList<PixelOcrImageCandidate> items)
    {
        Items = items;
    }

    public IReadOnlyList<PixelOcrImageCandidate> Items { get; }

    public void Dispose()
    {
        foreach (var candidate in Items)
        {
            candidate.Bitmap.Dispose();
        }
    }
}

public sealed record PixelOcrImageCandidate(string Name, SKBitmap Bitmap);
