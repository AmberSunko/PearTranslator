namespace PearTranslator.Core.Control;

public sealed class TranslatorController
{
    private string? _currentSourceText;
    private string? _dismissedSourceText;

    public TranslatorRunState State { get; private set; } = TranslatorRunState.Running;

    public bool ShouldCapture => State is TranslatorRunState.Running or TranslatorRunState.Dismissed;

    public bool ShouldShowOverlay => State == TranslatorRunState.Running;

    public void TogglePause()
    {
        if (State == TranslatorRunState.Paused)
        {
            Resume();
            return;
        }

        _dismissedSourceText = null;
        State = TranslatorRunState.Paused;
    }

    public void Resume()
    {
        if (State == TranslatorRunState.Paused)
        {
            State = TranslatorRunState.Running;
        }
    }

    public void RestoreDismissed()
    {
        if (State == TranslatorRunState.Dismissed)
        {
            _dismissedSourceText = null;
            State = TranslatorRunState.Running;
        }
    }

    public void DismissCurrent()
    {
        if (State != TranslatorRunState.Running)
        {
            return;
        }

        _dismissedSourceText = _currentSourceText;
        State = TranslatorRunState.Dismissed;
    }

    public void AcceptStableSourceText(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return;
        }

        _currentSourceText = sourceText;

        if (State == TranslatorRunState.Dismissed)
        {
            if (_dismissedSourceText is not null &&
                string.Equals(_dismissedSourceText, sourceText, StringComparison.Ordinal))
            {
                return;
            }

            _dismissedSourceText = null;
            State = TranslatorRunState.Running;
        }
    }
}
