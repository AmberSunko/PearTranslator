using System.IO;

namespace PearTranslator.App.Wpf.Tests;

public sealed class MainWindowHotkeyBehaviorTests
{
    [Fact]
    public void DismissHotkeyUsesCompositionToggle()
    {
        var source = File.ReadAllText(GetMainWindowCodePath());

        Assert.Contains("ToggleDismissCurrentAsync", source);
        Assert.DoesNotContain("_composition.Controller.DismissCurrent()", source);
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
