using PearTranslator.App.Wpf.Overlay;

namespace PearTranslator.App.Wpf.Tests;

public sealed class WindowDisplayAffinityStateTests
{
    [Fact]
    public void DoesNotRequestNativeUpdateWhenAffinityModeIsUnchanged()
    {
        var state = new WindowDisplayAffinityState();

        Assert.False(state.TryMarkRequested(WindowDisplayAffinityMode.None));
        Assert.True(state.TryMarkRequested(WindowDisplayAffinityMode.ExcludedFromCapture));
        Assert.False(state.TryMarkRequested(WindowDisplayAffinityMode.ExcludedFromCapture));
        Assert.True(state.TryMarkRequested(WindowDisplayAffinityMode.None));
        Assert.False(state.TryMarkRequested(WindowDisplayAffinityMode.None));
    }
}
