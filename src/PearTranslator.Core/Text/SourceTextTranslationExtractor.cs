using System.Text;
using PearTranslator.Core.Configuration;

namespace PearTranslator.Core.Text;

public static class SourceTextTranslationExtractor
{
    public static string ExtractTranslatableText(
        string text,
        TargetLanguage targetLanguage = TargetLanguage.SimplifiedChinese)
    {
        var normalized = TextNormalizer.NormalizePreservingLineBreaks(text);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        if (targetLanguage == TargetLanguage.SimplifiedChinese &&
            ContainsKanaOrHangul(normalized))
        {
            return normalized;
        }

        var builder = new StringBuilder(normalized.Length);
        foreach (var value in normalized)
        {
            builder.Append(ShouldRemoveForTarget(value, targetLanguage) ? ' ' : value);
        }

        return TextNormalizer.NormalizePreservingLineBreaks(builder.ToString());
    }

    private static bool ShouldRemoveForTarget(char value, TargetLanguage targetLanguage)
    {
        return targetLanguage switch
        {
            TargetLanguage.English => IsAsciiLetter(value) || IsAsciiPunctuation(value),
            _ => IsCjkLike(value) || IsCjkPunctuation(value)
        };
    }

    private static bool IsAsciiLetter(char value)
    {
        return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static bool IsAsciiPunctuation(char value)
    {
        return value is
            >= '\u0021' and <= '\u002F' or
            >= '\u003A' and <= '\u0040' or
            >= '\u005B' and <= '\u0060' or
            >= '\u007B' and <= '\u007E';
    }

    private static bool IsCjkLike(char value)
    {
        return value is
            >= '\u3400' and <= '\u4DBF' or
            >= '\u4E00' and <= '\u9FFF' or
            >= '\uF900' and <= '\uFAFF' or
            >= '\u3040' and <= '\u30FF' or
            >= '\uAC00' and <= '\uD7AF';
    }

    private static bool ContainsKanaOrHangul(string text)
    {
        return text.Any(IsKanaOrHangul);
    }

    private static bool IsKanaOrHangul(char value)
    {
        return value is
            >= '\u3040' and <= '\u30FF' or
            >= '\uAC00' and <= '\uD7AF';
    }

    private static bool IsCjkPunctuation(char value)
    {
        return value is
            >= '\u3000' and <= '\u303F' or
            >= '\uFF00' and <= '\uFFEF';
    }
}
