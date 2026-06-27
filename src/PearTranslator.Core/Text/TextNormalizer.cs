using System.Text.RegularExpressions;

namespace PearTranslator.Core.Text;

public static partial class TextNormalizer
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return WhitespacePattern().Replace(text.Trim(), " ");
    }

    public static string NormalizePreservingLineBreaks(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalizedLineBreaks = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalizedLineBreaks
            .Split('\n')
            .Select(line => HorizontalWhitespacePattern().Replace(line.Trim(), " "))
            .Where(line => line.Length > 0);

        return string.Join('\n', lines);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"[^\S\r\n]+")]
    private static partial Regex HorizontalWhitespacePattern();
}
