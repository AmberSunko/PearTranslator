using System.IO;
using System.Xml.Linq;

namespace PearTranslator.App.Wpf.Tests;

public sealed class MainWindowGlassBrushTests
{
    [Fact]
    public void GlassChromeBrushResourcesSeparateActiveAndInactiveColors()
    {
        var appXaml = XDocument.Load(GetAppXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var activeBrush = FindBrush(appXaml, presentation, xaml, "ActiveGlassChromeBrush");
        var inactiveBrush = FindBrush(appXaml, presentation, xaml, "InactiveGlassChromeBrush");
        var defaultBrush = FindBrush(appXaml, presentation, xaml, "GlassChromeBrush");

        Assert.Equal("#36EEF4F9", (string?)activeBrush.Attribute("Color"));
        Assert.Equal("#DCEEF4F9", (string?)inactiveBrush.Attribute("Color"));
        Assert.Equal("#36EEF4F9", (string?)defaultBrush.Attribute("Color"));
        Assert.Empty(appXaml.Descendants(presentation + "LinearGradientBrush")
            .Where(element => (string?)element.Attribute(xaml + "Key") == "GlassChromeBrush"));
    }

    [Fact]
    public void ContentPanelUsesOpaqueWhiteBackground()
    {
        var appXaml = XDocument.Load(GetAppXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var panelColor = appXaml.Descendants(presentation + "Color")
            .Single(element => (string?)element.Attribute(xaml + "Key") == "PanelColor");

        Assert.Equal("#FFFFFFFF", panelColor.Value);
    }

    [Fact]
    public void ContentPanelDoesNotCutIntoCloseButtonCorner()
    {
        var mainWindowXaml = XDocument.Load(GetMainWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var contentPanel = mainWindowXaml.Descendants(presentation + "Border")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "ContentPanel");

        Assert.Equal("16,0,16,16", (string?)contentPanel.Attribute("CornerRadius"));
    }

    [Fact]
    public void CloseButtonHoverBackgroundFillsTitleBarCell()
    {
        var appXaml = XDocument.Load(GetAppXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var closeButtonChrome = appXaml.Descendants(presentation + "Border")
            .Single(element =>
                (string?)element.Attribute(xaml + "Name") == "ButtonChrome" &&
                (string?)element.Attribute("CornerRadius") == "0");

        Assert.NotNull(closeButtonChrome);
    }

    private static XElement FindBrush(
        XDocument appXaml,
        XNamespace presentation,
        XNamespace xaml,
        string key)
    {
        return appXaml.Descendants(presentation + "SolidColorBrush")
            .Single(element => (string?)element.Attribute(xaml + "Key") == key);
    }

    private static string GetAppXamlPath()
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
            "App.xaml"));
    }

    private static string GetMainWindowXamlPath()
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
            "MainWindow.xaml"));
    }
}
