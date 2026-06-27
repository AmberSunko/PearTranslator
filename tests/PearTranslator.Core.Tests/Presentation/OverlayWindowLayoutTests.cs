using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace PearTranslator.Core.Tests.Presentation;

public sealed class OverlayWindowLayoutTests
{
    [Fact]
    public void OverlayToolbarButtonsAreCentered()
    {
        var xaml = XDocument.Load(GetOverlayWindowXamlPath());
        XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
        var controlsHost = xaml
            .Descendants()
            .Single(element => (string?)element.Attribute(xamlNamespace + "Name") == "OverlayControlsHost");

        Assert.Equal("Center", (string?)controlsHost.Attribute("HorizontalAlignment"));
    }

    [Fact]
    public void OverlayToolbarContainsCloseOneShotButton()
    {
        var xaml = XDocument.Load(GetOverlayWindowXamlPath());
        XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        var closeButton = xaml
            .Descendants()
            .Single(element => (string?)element.Attribute(xamlNamespace + "Name") == "CloseOneShotOverlayButton");

        Assert.Equal("OnCloseOneShotOverlayClicked", (string?)closeButton.Attribute("Click"));
    }

    private static string GetOverlayWindowXamlPath([CallerFilePath] string sourceFile = "")
    {
        var projectRoot = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!,
            "..",
            "..",
            ".."));

        return Path.Combine(projectRoot, "src", "PearTranslator.App.Wpf", "Overlay", "OverlayWindow.xaml");
    }
}
