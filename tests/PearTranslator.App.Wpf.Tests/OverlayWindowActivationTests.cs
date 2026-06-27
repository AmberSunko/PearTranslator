using System.IO;
using System.Xml.Linq;

namespace PearTranslator.App.Wpf.Tests;

public sealed class OverlayWindowActivationTests
{
    [Fact]
    public void OverlayWindowDoesNotActivateUnderlyingAppWhenShown()
    {
        var overlayXaml = XDocument.Load(GetOverlayWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var window = overlayXaml.Descendants(presentation + "Window").Single();
        var source = File.ReadAllText(GetOverlayWindowCodeBehindPath());

        Assert.Equal("False", (string?)window.Attribute("ShowActivated"));
        Assert.Contains("WsExNoactivate", source);
        Assert.Contains("WsExToolwindow | WsExNoactivate", source);
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
