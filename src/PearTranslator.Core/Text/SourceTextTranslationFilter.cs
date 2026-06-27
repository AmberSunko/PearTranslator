using PearTranslator.Core.Configuration;

namespace PearTranslator.Core.Text;

public static class SourceTextTranslationFilter
{
    private static readonly HashSet<string> StandaloneCodeBlockLanguageLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "text",
        "txt",
        "plaintext"
    };

    public static bool ShouldTranslate(
        string text,
        TargetLanguage targetLanguage = TargetLanguage.SimplifiedChinese)
    {
        var normalized = SourceTextTranslationExtractor.ExtractTranslatableText(text, targetLanguage);
        if (StandaloneCodeBlockLanguageLabels.Contains(normalized))
        {
            return false;
        }

        return targetLanguage switch
        {
            TargetLanguage.English => normalized.Any(IsNonAsciiLetter),
            _ => normalized.Any(IsAsciiLetter) || normalized.Any(IsKanaOrHangul)
        };
    }

    private static bool IsAsciiLetter(char value)
    {
        return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static bool IsNonAsciiLetter(char value)
    {
        return char.IsLetter(value) && !IsAsciiLetter(value);
    }

    private static bool IsKanaOrHangul(char value)
    {
        return value is
            >= '\u3040' and <= '\u30FF' or
            >= '\uAC00' and <= '\uD7AF';
    }
}
