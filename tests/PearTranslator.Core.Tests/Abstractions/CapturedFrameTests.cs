using PearTranslator.Core.Abstractions;

namespace PearTranslator.Core.Tests.Abstractions;

public sealed class CapturedFrameTests
{
    [Fact]
    public void CanCarryEncodedImagePayload()
    {
        var region = new FrameRegion(10, 20, 300, 90);
        var capturedAt = DateTimeOffset.UtcNow;

        var frame = new CapturedFrame(region, capturedAt, [1, 2, 3], [4, 5, 6], "image/png");

        Assert.Equal(region, frame.Region);
        Assert.Equal(capturedAt, frame.CapturedAt);
        Assert.Equal([1, 2, 3], frame.Fingerprint);
        Assert.Equal([4, 5, 6], frame.ImageBytes);
        Assert.Equal("image/png", frame.ImageMimeType);
        Assert.True(frame.HasImage);
        Assert.False(frame.HasRawBgra32Image);
    }

    [Fact]
    public void CanCarryRawBgra32ImagePayload()
    {
        var region = new FrameRegion(10, 20, 3, 2);
        var pixels = new byte[3 * 2 * 4];

        var frame = new CapturedFrame(
            region,
            DateTimeOffset.UtcNow,
            [1, 2, 3],
            pixels,
            CapturedFrame.RawBgra32MimeType,
            ImageWidthPixels: 3,
            ImageHeightPixels: 2);

        Assert.True(frame.HasImage);
        Assert.True(frame.HasRawBgra32Image);
        Assert.Equal(3, frame.ImageWidthPixels);
        Assert.Equal(2, frame.ImageHeightPixels);
    }

    [Fact]
    public void LegacyFramesWithoutImagePayloadAreSupported()
    {
        var frame = new CapturedFrame(new FrameRegion(0, 0, 100, 40), DateTimeOffset.UtcNow, [1]);

        Assert.Empty(frame.ImageBytes);
        Assert.Equal(string.Empty, frame.ImageMimeType);
        Assert.False(frame.HasImage);
        Assert.False(frame.HasRawBgra32Image);
    }
}
