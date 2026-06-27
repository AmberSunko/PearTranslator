using PearTranslator.Core.Abstractions;

namespace PearTranslator.Core.Pipeline;

public sealed class FingerprintFrameChangeDetector : IFrameChangeDetector
{
    private byte[]? _lastFingerprint;

    public bool HasMeaningfulChange(CapturedFrame frame)
    {
        if (_lastFingerprint is not null && frame.Fingerprint.AsSpan().SequenceEqual(_lastFingerprint))
        {
            return false;
        }

        _lastFingerprint = frame.Fingerprint.ToArray();
        return true;
    }
}
