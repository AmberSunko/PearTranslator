using PearTranslator.Core.Abstractions;

namespace PearTranslator.App.Wpf.Mocks;

public sealed class MockRegionCapture : IRegionCapture
{
    private FrameRegion? _region;
    private int _frameNumber;

    public bool HasRegion => _region.HasValue;

    public FrameRegion? CurrentRegion => _region;

    public void SetRegion(FrameRegion region)
    {
        _region = region;
    }

    public Task<CapturedFrame> CaptureAsync(CancellationToken cancellationToken)
    {
        if (_region is not { } region)
        {
            throw new InvalidOperationException("请先选择字幕区域，再开始截图。");
        }

        _frameNumber++;
        return Task.FromResult(new CapturedFrame(region, DateTimeOffset.UtcNow, BitConverter.GetBytes(_frameNumber)));
    }
}
