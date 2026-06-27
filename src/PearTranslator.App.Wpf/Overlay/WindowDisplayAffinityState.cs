namespace PearTranslator.App.Wpf.Overlay;

internal enum WindowDisplayAffinityMode
{
    None,
    ExcludedFromCapture
}

internal sealed class WindowDisplayAffinityState
{
    private WindowDisplayAffinityMode _currentMode = WindowDisplayAffinityMode.None;

    public bool TryMarkRequested(WindowDisplayAffinityMode requestedMode)
    {
        if (_currentMode == requestedMode)
        {
            return false;
        }

        _currentMode = requestedMode;
        return true;
    }
}
