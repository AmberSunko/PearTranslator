namespace PearTranslator.Core.Text;

public sealed class TextStabilizer
{
    private readonly int _requiredRepeats;
    private string? _lastText;
    private int _repeatCount;

    public TextStabilizer(int requiredRepeats)
    {
        if (requiredRepeats < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredRepeats), "Repeat count must be at least 1.");
        }

        _requiredRepeats = requiredRepeats;
    }

    public string? Observe(string text)
    {
        var normalized = TextNormalizer.NormalizePreservingLineBreaks(text);
        if (normalized.Length == 0)
        {
            return null;
        }

        if (string.Equals(_lastText, normalized, StringComparison.Ordinal))
        {
            _repeatCount++;
        }
        else
        {
            _lastText = normalized;
            _repeatCount = 1;
        }

        return _repeatCount >= _requiredRepeats ? normalized : null;
    }
}
