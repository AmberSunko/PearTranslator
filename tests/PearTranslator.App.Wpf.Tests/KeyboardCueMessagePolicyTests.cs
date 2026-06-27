using PearTranslator.App.Wpf;

namespace PearTranslator.App.Wpf.Tests;

public sealed class KeyboardCueMessagePolicyTests
{
    [Fact]
    public void SuppressesBareAltSystemKeyMessages()
    {
        Assert.True(KeyboardCueMessagePolicy.ShouldSuppress(
            KeyboardCueMessagePolicy.WmSysKeyDown,
            (IntPtr)KeyboardCueMessagePolicy.VkMenu));
        Assert.True(KeyboardCueMessagePolicy.ShouldSuppress(
            KeyboardCueMessagePolicy.WmSysKeyUp,
            (IntPtr)KeyboardCueMessagePolicy.VkMenu));
    }

    [Fact]
    public void DoesNotSuppressAltCombinedWithCommandKey()
    {
        Assert.False(KeyboardCueMessagePolicy.ShouldSuppress(
            KeyboardCueMessagePolicy.WmSysKeyDown,
            (IntPtr)'R'));
    }

    [Fact]
    public void SuppressesMessagesThatTryToShowKeyboardCues()
    {
        var showKeyboardCues = KeyboardCueMessagePolicy.MakeWParam(
            KeyboardCueMessagePolicy.UisClear,
            KeyboardCueMessagePolicy.UisfHideFocus | KeyboardCueMessagePolicy.UisfHideAccel);

        Assert.True(KeyboardCueMessagePolicy.ShouldSuppress(
            KeyboardCueMessagePolicy.WmChangeUiState,
            showKeyboardCues));
    }
}
