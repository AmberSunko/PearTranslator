using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Pipeline;

namespace PearTranslator.Core.Tests.Pipeline;

public sealed class FingerprintFrameChangeDetectorTests
{
    [Fact]
    public void FirstFrameCountsAsChanged()
    {
        var detector = new FingerprintFrameChangeDetector();

        var changed = detector.HasMeaningfulChange(CreateFrame([1, 2, 3]));

        Assert.True(changed);
    }

    [Fact]
    public void SameFingerprintDoesNotCountAsChanged()
    {
        var detector = new FingerprintFrameChangeDetector();

        detector.HasMeaningfulChange(CreateFrame([1, 2, 3]));
        var changed = detector.HasMeaningfulChange(CreateFrame([1, 2, 3]));

        Assert.False(changed);
    }

    [Fact]
    public void DifferentFingerprintCountsAsChanged()
    {
        var detector = new FingerprintFrameChangeDetector();

        detector.HasMeaningfulChange(CreateFrame([1, 2, 3]));
        var changed = detector.HasMeaningfulChange(CreateFrame([1, 2, 4]));

        Assert.True(changed);
    }

    private static CapturedFrame CreateFrame(byte[] fingerprint)
    {
        return new CapturedFrame(new FrameRegion(0, 0, 100, 40), DateTimeOffset.UtcNow, fingerprint);
    }
}
