using System.Text;

namespace PearTranslator.Core.Text;

public static class TranslationTextNormalizer
{
    public static string NormalizeForDisplay(string text)
    {
        var normalized = TextNormalizer.NormalizePreservingLineBreaks(text);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        return RemoveSpacesBetweenCjkCharacters(normalized);
    }

    private static string RemoveSpacesBetweenCjkCharacters(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var current = text[i];
            if (current == ' ' &&
                i > 0 &&
                i < text.Length - 1 &&
                IsCjkLike(text[i - 1]) &&
                IsCjkLike(text[i + 1]))
            {
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static bool IsCjkLike(char value)
    {
        return value is
            >= '\u3400' and <= '\u4DBF' or
            >= '\u4E00' and <= '\u9FFF' or
            >= '\uF900' and <= '\uFAFF' or
            >= '\u3040' and <= '\u30FF' or
            >= '\uAC00' and <= '\uD7AF' or
            >= '\u3000' and <= '\u303F' or
            >= '\uFF00' and <= '\uFFEF';
    }
}
