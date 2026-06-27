namespace PearTranslator.App.Wpf;

public static class WindowChromeMetrics
{
    public static double CalculateTopGlassHeight(
        double titleBarHeight,
        double contentTopLeftRadius,
        double contentTopRightRadius)
    {
        var contentTopCornerHeight = Math.Max(
            Math.Max(0, contentTopLeftRadius),
            Math.Max(0, contentTopRightRadius));

        return Math.Max(0, titleBarHeight) + contentTopCornerHeight;
    }

    public static double CalculateBottomGlassHeight(
        double actualFooterHeight,
        double marginTop,
        double marginBottom,
        double minimumHeight,
        double contentBottomMargin = 0,
        double contentBottomLeftRadius = 0,
        double contentBottomRightRadius = 0)
    {
        var contentBottomCornerHeight = Math.Max(
            Math.Max(0, contentBottomLeftRadius),
            Math.Max(0, contentBottomRightRadius));
        var footerBandHeight = Math.Max(0, actualFooterHeight) +
            Math.Max(0, marginTop) +
            Math.Max(0, marginBottom) +
            Math.Max(0, contentBottomMargin) +
            contentBottomCornerHeight;

        return Math.Max(Math.Max(0, minimumHeight), footerBandHeight);
    }
}
