namespace PearTranslator.Core.Abstractions;

public interface IRegionCapture
{
    FrameRegion? CurrentRegion => null;

    Task<CapturedFrame> CaptureAsync(CancellationToken cancellationToken);
}
