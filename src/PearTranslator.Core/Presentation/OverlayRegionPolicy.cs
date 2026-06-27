using PearTranslator.Core.Abstractions;

namespace PearTranslator.Core.Presentation;

public static class OverlayRegionPolicy
{
    public static bool Overlaps(FrameRegion first, FrameRegion second)
    {
        var firstRight = first.X + first.Width;
        var firstBottom = first.Y + first.Height;
        var secondRight = second.X + second.Width;
        var secondBottom = second.Y + second.Height;

        return first.X < secondRight &&
            firstRight > second.X &&
            first.Y < secondBottom &&
            firstBottom > second.Y;
    }
}
