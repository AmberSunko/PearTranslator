using PearTranslator.Ocr.Windows;
using SkiaSharp;

namespace PearTranslator.Ocr.Windows.Tests;

public sealed class PixelOcrImagePreprocessorTests
{
    [Fact]
    public void CreateCandidatesIncludesOnlyGrayVariant()
    {
        using var bitmap = CreateSampleBitmap(darkText: false);

        using var candidates = PixelOcrImagePreprocessor.CreateCandidates(bitmap);

        Assert.Collection(
            candidates.Items,
            candidate =>
            {
                Assert.Equal("gray-1x", candidate.Name);
                Assert.Equal(12, candidate.Bitmap.Width);
                Assert.Equal(8, candidate.Bitmap.Height);
                AssertGrayscale(candidate.Bitmap);
            });
    }

    [Fact]
    public void GrayVariantHandlesDarkAndLightTextWithoutBinarizing()
    {
        using var lightText = CreateSampleBitmap(darkText: false);
        using var darkText = CreateSampleBitmap(darkText: true);

        using var lightCandidates = PixelOcrImagePreprocessor.CreateCandidates(lightText);
        using var darkCandidates = PixelOcrImagePreprocessor.CreateCandidates(darkText);

        AssertGrayscale(lightCandidates.Items[0].Bitmap);
        AssertGrayscale(darkCandidates.Items[0].Bitmap);
        Assert.Contains(lightCandidates.Items[0].Bitmap.Pixels, color => color.Red == 255);
        Assert.Contains(darkCandidates.Items[0].Bitmap.Pixels, color => color.Red == 0);
    }

    [Fact]
    public void CreateCandidatesDownscalesLargeRealtimeRegionsBeforeOcr()
    {
        using var bitmap = new SKBitmap(1920, 1080);
        bitmap.Erase(SKColors.Black);

        using var candidates = PixelOcrImagePreprocessor.CreateCandidates(bitmap);

        Assert.Collection(
            candidates.Items,
            candidate =>
            {
                Assert.Equal("gray-fit", candidate.Name);
                Assert.True(candidate.Bitmap.Width <= 900);
                Assert.True(candidate.Bitmap.Width * candidate.Bitmap.Height <= 450_000);
                Assert.InRange(
                    candidate.Bitmap.Width / (double)candidate.Bitmap.Height,
                    (bitmap.Width / (double)bitmap.Height) - 0.01,
                    (bitmap.Width / (double)bitmap.Height) + 0.01);
            });
    }

    [Fact]
    public void CreateCandidatesUsesNearestNeighborWhenDownscaling()
    {
        using var bitmap = new SKBitmap(1200, 2);
        for (var x = 0; x < bitmap.Width; x++)
        {
            var color = x % 2 == 0 ? SKColors.Black : SKColors.White;
            bitmap.SetPixel(x, 0, color);
            bitmap.SetPixel(x, 1, color);
        }

        using var candidates = PixelOcrImagePreprocessor.CreateCandidates(bitmap);

        var candidate = candidates.Items[0].Bitmap;
        Assert.Equal(900, candidate.Width);
        Assert.Equal(2, candidate.Height);
        Assert.Equal(0, candidate.GetPixel(0, 0).Red);
        Assert.Equal(255, candidate.GetPixel(1, 0).Red);
        Assert.Equal(0, candidate.GetPixel(3, 0).Red);
    }

    private static SKBitmap CreateSampleBitmap(bool darkText)
    {
        var bitmap = new SKBitmap(12, 8);
        var background = darkText ? SKColors.White : SKColors.Black;
        var foreground = darkText ? SKColors.Black : SKColors.White;
        bitmap.Erase(background);

        for (var y = 2; y <= 5; y++)
        {
            for (var x = 2; x <= 9; x++)
            {
                bitmap.SetPixel(x, y, foreground);
            }
        }

        return bitmap;
    }

    private static void AssertGrayscale(SKBitmap bitmap)
    {
        foreach (var color in bitmap.Pixels)
        {
            Assert.Equal(color.Red, color.Green);
            Assert.Equal(color.Red, color.Blue);
            Assert.Equal(255, color.Alpha);
        }
    }
}
