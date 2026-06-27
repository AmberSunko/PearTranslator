using System.IO;
using System.Xml.Linq;

namespace PearTranslator.App.Wpf.Tests;

public sealed class RuntimeAssetsUiTests
{
    [Fact]
    public void MainWindowContainsConfigureModelsButtonAndStatusText()
    {
        var xaml = XDocument.Load(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "PearTranslator.App.Wpf",
            "MainWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var button = xaml.Descendants(presentation + "Button")
            .Single(element => (string?)element.Attribute(x + "Name") == "ConfigureAssetsButton");
        var statusText = xaml.Descendants(presentation + "TextBlock")
            .Single(element => (string?)element.Attribute(x + "Name") == "RuntimeAssetsStatusText");

        Assert.Equal("配置模型", (string?)button.Attribute("Content"));
        Assert.Equal("OnConfigureAssetsClicked", (string?)button.Attribute("Click"));
        Assert.NotNull(statusText);
    }

    [Fact]
    public void MainWindowCodeBehindHandlesConfigureModelsButton()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "PearTranslator.App.Wpf",
            "MainWindow.xaml.cs"));

        Assert.Contains("OnConfigureAssetsClicked", source);
        Assert.Contains("ConfigureRuntimeAssetsAsync", source);
        Assert.Contains("UpdateRuntimeAssetStatus", source);
    }
}
