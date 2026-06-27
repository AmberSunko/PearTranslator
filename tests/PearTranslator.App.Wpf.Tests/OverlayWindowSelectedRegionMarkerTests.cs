using System.IO;
using System.Xml.Linq;

namespace PearTranslator.App.Wpf.Tests;

public sealed class OverlayWindowSelectedRegionMarkerTests
{
    [Fact]
    public void OverlayToolbarHasSelectedRegionMarkerToggleButton()
    {
        var overlayXaml = XDocument.Load(GetOverlayWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var markerButton = overlayXaml.Descendants(presentation + "Button")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "SelectedRegionMarkerButton");

        Assert.Equal("\u6846", (string?)markerButton.Attribute("Content"));
        Assert.Equal("OnToggleSelectedRegionMarkerClicked", (string?)markerButton.Attribute("Click"));
        Assert.Equal("2,0,0,0", (string?)markerButton.Attribute("Margin"));
    }

    [Fact]
    public void SelectedRegionMarkerButtonRaisesPresenterLevelToggleAndIsOffByDefault()
    {
        var source = File.ReadAllText(GetOverlayWindowCodeBehindPath());

        Assert.Contains("private bool _selectedRegionMarkerEnabled;", source);
        Assert.Contains("public event EventHandler<bool>? SelectedRegionMarkerToggled;", source);
        Assert.Contains("OnToggleSelectedRegionMarkerClicked", source);
        Assert.Contains("_selectedRegionMarkerEnabled = !_selectedRegionMarkerEnabled;", source);
        Assert.Contains("SelectedRegionMarkerToggled?.Invoke(this, _selectedRegionMarkerEnabled);", source);
        Assert.Contains("public void SetSelectedRegionMarkerEnabled(bool isEnabled)", source);
        Assert.Contains("UpdateSelectedRegionMarkerButtonState", source);
        Assert.DoesNotContain("SelectedRegionDebugRectangle", source);
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
