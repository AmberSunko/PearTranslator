using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;

namespace PearTranslator.Core.Text;

public static class OcrLineTextExtractor
{
    public static OcrTextLine[] GetTranslatableBoundedLines(
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        TargetLanguage targetLanguage = TargetLanguage.SimplifiedChinese)
    {
        return sourceTextLines?
            .Where(line => line.BoundsPixels.HasValue &&
                SourceTextTranslationFilter.ShouldTranslate(line.Text, targetLanguage))
            .ToArray() ?? [];
    }

    public static string[] GetTranslatableBoundedLineTexts(
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        TargetLanguage targetLanguage = TargetLanguage.SimplifiedChinese)
    {
        return GetTranslatableBoundedLines(sourceTextLines, targetLanguage)
            .Select(line => SourceTextTranslationExtractor.ExtractTranslatableText(line.Text, targetLanguage))
            .Where(line => line.Length > 0)
            .ToArray();
    }

    public static string[] GetTranslatableFallbackLineTexts(
        string fallbackSourceText,
        TargetLanguage targetLanguage = TargetLanguage.SimplifiedChinese)
    {
        return TextNormalizer.NormalizePreservingLineBreaks(fallbackSourceText)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => SourceTextTranslationExtractor.ExtractTranslatableText(line, targetLanguage))
            .Where(line => line.Length > 0 && SourceTextTranslationFilter.ShouldTranslate(line, targetLanguage))
            .ToArray();
    }
}
