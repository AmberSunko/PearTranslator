using System.IO;
using System.Xml.Linq;

namespace PearTranslator.App.Wpf.Tests;

public sealed class SelectionMarkerWindowTests
{
    [Fact]
    public void SelectionMarkerWindowDrawsOnlyTheSelectedScreenRegion()
    {
        var xaml = XDocument.Load(GetSelectionMarkerWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        var window = xaml.Root!;
        var markerRectangle = xaml.Descendants(presentation + "Rectangle")
            .Single(element => (string?)element.Attribute(xamlNamespace + "Name") == "MarkerRectangle");

        Assert.Equal("None", (string?)window.Attribute("WindowStyle"));
        Assert.Equal("True", (string?)window.Attribute("AllowsTransparency"));
        Assert.Equal("Transparent", (string?)window.Attribute("Background"));
        Assert.Equal("False", (string?)window.Attribute("ShowActivated"));
        Assert.Equal("#556DEA", (string?)markerRectangle.Attribute("Stroke"));
        Assert.Equal("1.5", (string?)markerRectangle.Attribute("StrokeThickness"));
        Assert.Equal("4 3", (string?)markerRectangle.Attribute("StrokeDashArray"));
        Assert.Equal("Transparent", (string?)markerRectangle.Attribute("Fill"));
    }

    [Fact]
    public void SelectionMarkerWindowPositionsItselfFromFrameRegionInsteadOfOverlayCanvas()
    {
        var source = File.ReadAllText(GetSelectionMarkerWindowCodeBehindPath());

        Assert.Contains("public void ShowRegion(FrameRegion region)", source);
        Assert.Contains("Left = region.X / dpi.DpiScaleX;", source);
        Assert.Contains("Top = region.Y / dpi.DpiScaleY;", source);
        Assert.Contains("Width = Math.Max(1, region.Width / dpi.DpiScaleX);", source);
        Assert.Contains("Height = Math.Max(1, region.Height / dpi.DpiScaleY);", source);
        Assert.DoesNotContain("CaptionCanvas", source);
        Assert.DoesNotContain("OverlayLayoutCalculator", source);
    }

    private static string GetSelectionMarkerWindowXamlPath()
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
            "SelectionMarkerWindow.xaml"));
    }

    private static string GetSelectionMarkerWindowCodeBehindPath()
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
            "SelectionMarkerWindow.xaml.cs"));
    }
}
