using System.IO;
using System.Xml.Linq;

namespace PearTranslator.App.Wpf.Tests;

public sealed class OverlayWindowBackgroundTests
{
    [Fact]
    public void OverlayBackgroundUsesSeparateSingleLayerToolbarAndDisplayBrushes()
    {
        var overlayXaml = XDocument.Load(GetOverlayWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var captionHost = overlayXaml.Descendants(presentation + "Border")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "CaptionHost");
        var toolbarHost = overlayXaml.Descendants(presentation + "Grid")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "ToolbarHost");
        var displayBackgroundHost = overlayXaml.Descendants(presentation + "Border")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "DisplayBackgroundHost");

        Assert.Equal("Transparent", (string?)captionHost.Attribute("Background"));
        Assert.Equal("#E6202024", (string?)toolbarHost.Attribute("Background"));
        Assert.Equal("#D0202024", (string?)displayBackgroundHost.Attribute("Background"));
    }

    [Fact]
    public void OverlayBackgroundModeChangesDisplayAreaInsteadOfOuterShell()
    {
        var source = File.ReadAllText(GetOverlayWindowCodeBehindPath());

        Assert.Contains("DisplayBackgroundHost.Background", source);
        Assert.DoesNotContain("CaptionHost.Background = _backgroundMode", source);
    }

    [Fact]
    public void OverlayToolbarSupportsUiLanguageTooltips()
    {
        var overlayXaml = XDocument.Load(GetOverlayWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var source = File.ReadAllText(GetOverlayWindowCodeBehindPath());

        Assert.Contains(overlayXaml.Descendants(presentation + "Button"),
            element => (string?)element.Attribute(xaml + "Name") == "DragOverlayButton");
        Assert.Contains(overlayXaml.Descendants(presentation + "Button"),
            element => (string?)element.Attribute(xaml + "Name") == "BackgroundModeButton");
        Assert.Contains("public UiLanguage UiLanguage", source);
        Assert.Contains("UpdateLocalizedTooltips", source);
        Assert.Contains("Disable text outline", source);
    }

    private static string GetOverlayWindowXamlPath()
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
            "OverlayWindow.xaml"));
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
