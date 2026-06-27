using System.IO;

namespace PearTranslator.App.Wpf.Tests;

public sealed class DualOverlayPresenterSelectedRegionMarkerTests
{
    [Fact]
    public void PresenterOwnsSeparateRealtimeAndOneShotSelectionMarkerWindows()
    {
        var source = File.ReadAllText(GetDualOverlayPresenterPath());

        Assert.Contains("private readonly SelectionMarkerWindow _realtimeSelectionMarker;", source);
        Assert.Contains("private readonly SelectionMarkerWindow _oneShotSelectionMarker;", source);
        Assert.Contains("_realtimeWindow.SelectedRegionMarkerToggled += OnSelectedRegionMarkerToggled;", source);
        Assert.Contains("_oneShotWindow.SelectedRegionMarkerToggled += OnSelectedRegionMarkerToggled;", source);
    }

    [Fact]
    public void PresenterShowsAndHidesMarkersWithTheirMatchingOverlayRegions()
    {
        var source = File.ReadAllText(GetDualOverlayPresenterPath());

        Assert.Contains("ShowRealtimeSelectionMarker(region)", source);
        Assert.Contains("_realtimeSelectionMarker.Hide();", source);
        Assert.Contains("ShowOneShotSelectionMarker(anchorRegion)", source);
        Assert.Contains("_oneShotSelectionMarker.Hide();", source);
        Assert.Contains("_realtimeWindow.SetSelectedRegionMarkerEnabled(_selectedRegionMarkerEnabled);", source);
        Assert.Contains("_oneShotWindow.SetSelectedRegionMarkerEnabled(_selectedRegionMarkerEnabled);", source);
    }

    private static string GetDualOverlayPresenterPath()
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
            "DualOverlayPresenter.cs"));
    }
}
