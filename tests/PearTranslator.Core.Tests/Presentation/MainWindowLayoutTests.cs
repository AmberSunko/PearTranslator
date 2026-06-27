using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace PearTranslator.Core.Tests.Presentation;

public sealed class MainWindowLayoutTests
{
    [Fact]
    public void MainWindowContainsOneShotDisplayDurationSetting()
    {
        var xaml = XDocument.Load(GetMainWindowXamlPath());
        XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        var comboBox = xaml
            .Descendants()
            .Single(element => (string?)element.Attribute(xamlNamespace + "Name") == "OneShotDisplayComboBox");

        Assert.Equal("Name", (string?)comboBox.Attribute("DisplayMemberPath"));
        Assert.Equal("Seconds", (string?)comboBox.Attribute("SelectedValuePath"));
    }

    [Fact]
    public void MainWindowContainsOverlayCaptureExclusionSetting()
    {
        var xaml = XDocument.Load(GetMainWindowXamlPath());
        XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        var checkBox = xaml
            .Descendants()
            .Single(element => (string?)element.Attribute(xamlNamespace + "Name") == "ExcludeOverlayFromCaptureCheckBox");

        Assert.Equal("不捕获覆盖层", (string?)checkBox.Attribute("Content"));
    }

    [Fact]
    public void MainWindowSettingsScrollViewerDoesNotShowFocusRectangle()
    {
        var xaml = XDocument.Load(GetMainWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        var scrollViewer = xaml
            .Descendants(presentation + "ScrollViewer")
            .Single();

        Assert.Equal("False", (string?)scrollViewer.Attribute("Focusable"));
        Assert.Equal("{x:Null}", (string?)scrollViewer.Attribute("FocusVisualStyle"));
    }

    [Fact]
    public void MainWindowUsesGroupedSettingsSections()
    {
        var xaml = XDocument.Load(GetMainWindowXamlPath());

        var groupedSections = xaml
            .Descendants()
            .Where(element => (string?)element.Attribute("Style") == "{StaticResource SettingsGroupBorderStyle}")
            .ToArray();

        Assert.True(groupedSections.Length >= 3);
        Assert.Contains(xaml.Descendants(), element => (string?)element.Attribute("Style") == "{StaticResource SettingsRowGridStyle}");
    }

    [Fact]
    public void MainWindowKeepsShortcutHeaderOutsideScrollableSettings()
    {
        var xaml = XDocument.Load(GetMainWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        var scrollViewer = xaml
            .Descendants(presentation + "ScrollViewer")
            .Single();
        var shortcutHeader = xaml
            .Descendants()
            .Single(element => (string?)element.Attribute(xamlNamespace + "Name") == "ShortcutTextBlock");

        Assert.DoesNotContain(shortcutHeader, scrollViewer.Descendants());
        Assert.Contains(scrollViewer.Descendants(presentation + "TextBlock"), element => (string?)element.Attribute("Text") == "基础");
    }

    [Fact]
    public void MainWindowSettingsScrollViewerUsesRoundedScrollBarStyle()
    {
        var xaml = XDocument.Load(GetMainWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var scrollViewer = xaml
            .Descendants(presentation + "ScrollViewer")
            .Single();

        Assert.Contains(scrollViewer.Descendants(presentation + "Style"),
            element => (string?)element.Attribute("BasedOn") == "{StaticResource SettingsScrollBarStyle}");
    }

    private static string GetMainWindowXamlPath([CallerFilePath] string sourceFile = "")
    {
        var projectRoot = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!,
            "..",
            "..",
            ".."));

        return Path.Combine(projectRoot, "src", "PearTranslator.App.Wpf", "MainWindow.xaml");
    }
}
