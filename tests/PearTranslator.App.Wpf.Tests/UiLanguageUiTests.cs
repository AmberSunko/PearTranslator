using System.IO;
using System.Xml.Linq;
using PearTranslator.App.Wpf;
using PearTranslator.Core.Configuration;

namespace PearTranslator.App.Wpf.Tests;

public sealed class UiLanguageUiTests
{
    [Fact]
    public void MainWindowContainsUiLanguageComboBox()
    {
        var mainWindow = XDocument.Load(GetMainWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var comboBox = mainWindow.Descendants(presentation + "ComboBox")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "UiLanguageComboBox");

        Assert.Equal("Language", (string?)comboBox.Attribute("SelectedValuePath"));
    }

    [Fact]
    public void MainWindowCodeReadsWritesAndAppliesUiLanguageSetting()
    {
        var source = File.ReadAllText(GetMainWindowCodePath());

        Assert.Contains("UiLanguageComboBox.ItemsSource", source);
        Assert.Contains("UiLanguageComboBox.SelectedValue = _settings.Appearance.UiLanguage", source);
        Assert.Contains("UiLanguage = UiLanguageComboBox.SelectedValue is UiLanguage", source);
        Assert.Contains("ApplyUiLanguage()", source);
    }

    [Fact]
    public void TextCatalogContainsChineseAndEnglishLabels()
    {
        var chinese = MainWindowTextCatalog.For(UiLanguage.SimplifiedChinese);
        var english = MainWindowTextCatalog.For(UiLanguage.English);

        Assert.Equal("界面语言", chinese.UiLanguageTitle);
        Assert.Equal("UI Language", english.UiLanguageTitle);
        Assert.Equal("选择区域", chinese.SelectRegionButton);
        Assert.Equal("Select Region", english.SelectRegionButton);
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

    private static string GetMainWindowCodePath()
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
            "MainWindow.xaml.cs"));
    }
}
