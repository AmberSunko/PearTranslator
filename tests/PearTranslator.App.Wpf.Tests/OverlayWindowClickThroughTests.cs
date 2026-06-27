using System.IO;

namespace PearTranslator.App.Wpf.Tests;

public sealed class OverlayWindowClickThroughTests
{
    [Fact]
    public void OverlayUsesDynamicTransparentStyleForCrossProcessClickThrough()
    {
        var source = File.ReadAllText(GetOverlayWindowCodeBehindPath());

        Assert.Contains("HtTransparent", source);
        Assert.Contains("IsPointInsideOverlayControls", source);
        Assert.Contains("WsExTransparent", source);
        Assert.Contains("SetClickThroughMode", source);
        Assert.Contains("_clickThroughTimer", source);
    }

    private static string GetOverlayWindowCodeBehindPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "PearTranslator.App.Wpf",
            "Overlay",
            "OverlayWindow.xaml.cs"));
    }
}
