using System.IO;
using System.Xml.Linq;

namespace PearTranslator.App.Wpf.Tests;

public sealed class RegionSelectionWindowTests
{
    [Fact]
    public void RegionSelectionWindowHandlesEscapeToCancelSelection()
    {
        var xaml = XDocument.Load(GetRegionSelectionWindowXamlPath());
        var source = File.ReadAllText(GetRegionSelectionWindowCodeBehindPath());

        Assert.Equal("OnKeyDown", (string?)xaml.Root!.Attribute("KeyDown"));
        Assert.Contains("if (e.Key != Key.Escape)", source);
        Assert.Contains("DialogResult = false;", source);
        Assert.Contains("Close();", source);
    }

    private static string GetRegionSelectionWindowXamlPath()
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
            "RegionSelection",
            "RegionSelectionWindow.xaml"));
    }

    private static string GetRegionSelectionWindowCodeBehindPath()
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
            "RegionSelection",
            "RegionSelectionWindow.xaml.cs"));
    }
}
