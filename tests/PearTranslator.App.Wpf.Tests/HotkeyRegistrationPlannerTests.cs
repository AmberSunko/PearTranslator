using System.Windows.Input;
using PearTranslator.App.Wpf.Hotkeys;

namespace PearTranslator.App.Wpf.Tests;

public sealed class HotkeyRegistrationPlannerTests
{
    [Fact]
    public void SelectRegionDoesNotRegisterShiftFallbackWhenPrimaryHotkeyIsUnavailable()
    {
        var registrar = new FakeHotkeyRegistrar(
            modifiersToFail: HotkeyRegistrationPlanner.ModControl | HotkeyRegistrationPlanner.ModAlt,
            virtualKeyToFail: (uint)KeyInterop.VirtualKeyFromKey(Key.R));

        var state = HotkeyRegistrationPlanner.RegisterDefaults(registrar, IntPtr.Zero);

        var selectRegionRegistrations = state.Registrations.Where(registration =>
            registration.Command == HotkeyCommand.SelectRegion);

        Assert.Empty(selectRegionRegistrations);
        Assert.Single(state.Failures.Where(failure => failure.Command == HotkeyCommand.SelectRegion));
        Assert.Contains("选择区域：Ctrl+Alt+R", state.ShortcutSummary);
        Assert.DoesNotContain(registrar.Attempts, attempt =>
            attempt.Modifiers == (HotkeyRegistrationPlanner.ModControl | HotkeyRegistrationPlanner.ModAlt | HotkeyRegistrationPlanner.ModShift));
    }

    private sealed class FakeHotkeyRegistrar(uint modifiersToFail, uint virtualKeyToFail) : IHotkeyRegistrar
    {
        public List<(uint Modifiers, uint VirtualKey)> Attempts { get; } = [];

        public bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey)
        {
            Attempts.Add((modifiers, virtualKey));
            return modifiers != modifiersToFail || virtualKey != virtualKeyToFail;
        }

        public bool UnregisterHotKey(IntPtr hWnd, int id)
        {
            return true;
        }
    }
}
