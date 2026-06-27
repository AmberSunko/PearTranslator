using PearTranslator.Core.Text;

namespace PearTranslator.Core.Translation;

public sealed class TranslationCache
{
    private readonly Dictionary<string, string> _translations = new(StringComparer.Ordinal);

    public bool TryGet(string sourceText, out string translation)
    {
        var key = TextNormalizer.NormalizePreservingLineBreaks(sourceText);
        if (key.Length == 0)
        {
            translation = string.Empty;
            return false;
        }

        if (_translations.TryGetValue(key, out var cached))
        {
            translation = cached;
            return true;
        }

        translation = string.Empty;
        return false;
    }

    public void Store(string sourceText, string translation)
    {
        var key = TextNormalizer.NormalizePreservingLineBreaks(sourceText);
        var value = TranslationTextNormalizer.NormalizeForDisplay(translation);

        if (key.Length == 0 || value.Length == 0)
        {
            return;
        }

        _translations[key] = value;
    }
}
