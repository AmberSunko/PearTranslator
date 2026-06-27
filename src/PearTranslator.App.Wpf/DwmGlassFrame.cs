namespace PearTranslator.App.Wpf;

public readonly record struct DwmGlassFrame(int TopPixels, int BottomPixels)
{
    public static DwmGlassFrame FromDeviceIndependentPixels(
        double topDip,
        double bottomDip,
        double dpiScaleY)
    {
        return new DwmGlassFrame(
            ToPhysicalPixels(topDip, dpiScaleY),
            ToPhysicalPixels(bottomDip, dpiScaleY));
    }

    private static int ToPhysicalPixels(double value, double dpiScale)
    {
        if (value <= 0 || dpiScale <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(value * dpiScale);
    }
}
