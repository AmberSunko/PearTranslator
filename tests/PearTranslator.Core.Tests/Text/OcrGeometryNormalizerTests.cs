using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Text;

namespace PearTranslator.Core.Tests.Text;

public sealed class OcrGeometryNormalizerTests
{
    [Fact]
    public void NormalizeScalesOcrBoundsFromImagePixelsToCaptureRegionPixels()
    {
        var captureRegion = new FrameRegion(100, 200, 800, 240);
        var imageBounds = new FrameRegion(125, 75, 500, 60);

        var geometry = OcrGeometryNormalizer.Normalize(
            captureRegion,
            imageWidthPixels: 1000,
            imageHeightPixels: 300,
            imageTextBoundsPixels: imageBounds,
            imageTextHeightPixels: 60);

        Assert.Equal(new FrameRegion(100, 60, 400, 48), geometry.TextBoundsPixels);
        Assert.Equal(48, geometry.EstimatedTextHeightPixels);
    }

    [Fact]
    public void NormalizeKeepsGeometryWhenCaptureAndImageSizeAlreadyMatch()
    {
        var captureRegion = new FrameRegion(100, 200, 800, 240);
        var imageBounds = new FrameRegion(100, 60, 400, 48);

        var geometry = OcrGeometryNormalizer.Normalize(
            captureRegion,
            imageWidthPixels: 800,
            imageHeightPixels: 240,
            imageTextBoundsPixels: imageBounds,
            imageTextHeightPixels: 48);

        Assert.Equal(imageBounds, geometry.TextBoundsPixels);
        Assert.Equal(48, geometry.EstimatedTextHeightPixels);
    }

    [Fact]
    public void NormalizeScalesEachOcrLineBoundsFromImagePixelsToCaptureRegionPixels()
    {
        var captureRegion = new FrameRegion(100, 200, 800, 240);
        var imageLines = new[]
        {
            new OcrTextLine("Open the door", new FrameRegion(125, 75, 500, 30)),
            new OcrTextLine("Take the key", new FrameRegion(125, 125, 450, 30))
        };

        var geometry = OcrGeometryNormalizer.Normalize(
            captureRegion,
            imageWidthPixels: 1000,
            imageHeightPixels: 300,
            imageTextBoundsPixels: null,
            imageTextHeightPixels: null,
            imageTextLines: imageLines);

        Assert.Collection(
            geometry.TextLines,
            line =>
            {
                Assert.Equal("Open the door", line.Text);
                Assert.Equal(new FrameRegion(100, 60, 400, 24), line.BoundsPixels);
            },
            line =>
            {
                Assert.Equal("Take the key", line.Text);
                Assert.Equal(new FrameRegion(100, 100, 360, 24), line.BoundsPixels);
            });
    }
}
