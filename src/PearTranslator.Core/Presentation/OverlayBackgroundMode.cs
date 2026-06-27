namespace PearTranslator.Core.Presentation;

public enum OverlayBackgroundMode
{
    Full,
    Transparent,
    SourceRegions
}

public static class OverlayBackgroundModeCycler
{
    public static OverlayBackgroundMode Next(OverlayBackgroundMode mode)
    {
        return mode switch
        {
            OverlayBackgroundMode.Full => OverlayBackgroundMode.Transparent,
            OverlayBackgroundMode.Transparent => OverlayBackgroundMode.SourceRegions,
            _ => OverlayBackgroundMode.Full
        };
    }
}
