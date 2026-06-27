using System.IO;
using System.Xml.Linq;

namespace PearTranslator.App.Wpf.Tests;

public sealed class TranslationTargetLanguageUiTests
{
    [Fact]
    public void MainWindowContainsTargetLanguageComboBox()
    {
        var mainWindow = XDocument.Load(GetMainWindowXamlPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        var comboBox = mainWindow.Descendants(presentation + "ComboBox")
            .Single(element => (string?)element.Attribute(xaml + "Name") == "TargetLanguageComboBox");

        Assert.Equal("Language", (string?)comboBox.Attribute("SelectedValuePath"));
    }

    [Fact]
    public void MainWindowCodeReadsAndWritesTargetLanguageSetting()
    {
        var source = File.ReadAllText(GetMainWindowCodePath());

        Assert.Contains("TargetLanguageComboBox.ItemsSource", source);
        Assert.Contains("TargetLanguageComboBox.SelectedValue = _settings.Translation.TargetLanguage", source);
        Assert.Contains("TargetLanguage = TargetLanguageComboBox.SelectedValue is TargetLanguage", source);
        Assert.Contains("new OcrLanguageChoice(OcrLanguageKind.Auto", source);
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
