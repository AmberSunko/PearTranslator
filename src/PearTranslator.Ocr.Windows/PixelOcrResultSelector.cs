using System.Text.RegularExpressions;
using PearTranslator.Core.Abstractions;

namespace PearTranslator.Ocr.Windows;

public static partial class PixelOcrResultSelector
{
    public static OcrResult SelectBest(IReadOnlyList<OcrResult> results)
    {
        if (results.Count == 0)
        {
            return new OcrResult(string.Empty);
        }

        return results
            .OrderByDescending(Score)
            .ThenByDescending(result => result.Text.Length)
            .First();
    }

    private static double Score(OcrResult result)
    {
        var text = result.Text.Trim();
        if (text.Length == 0)
        {
            return 0;
        }

        var letterCount = text.Count(char.IsLetter);
        var digitCount = text.Count(char.IsDigit);
        var whitespaceCount = text.Count(char.IsWhiteSpace);
        var punctuationCount = text.Count(char.IsPunctuation);
        var symbolCount = text.Length - letterCount - digitCount - whitespaceCount - punctuationCount;
        var wordCount = EnglishWordPattern().Matches(text).Count;
        var lineCount = result.TextLines?.Count(line => !string.IsNullOrWhiteSpace(line.Text)) ?? 0;
        var symbolNoise = punctuationCount + symbolCount;

        return (letterCount * 1.4) +
            (wordCount * 8.0) +
            (Math.Min(whitespaceCount, 8) * 1.5) +
            (lineCount * 2.0) -
            (symbolNoise * 2.5);
    }

    [GeneratedRegex(@"[A-Za-z][A-Za-z']{1,}")]
    private static partial Regex EnglishWordPattern();
}
