namespace PearTranslator.Core.Abstractions;

public interface IFrameChangeDetector
{
    bool HasMeaningfulChange(CapturedFrame frame);
}
