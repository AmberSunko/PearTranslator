using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Text;

namespace PearTranslator.Ocr.Windows;

internal static class MixedLanguageOcrResultMerger
{
    private const double OverlapReplacementThreshold = 0.45;

    public static OcrResult Merge(OcrResult primary, OcrResult korean)
    {
        var lines = new List<OcrTextLine>();
        foreach (var line in ReadLines(primary))
        {
            AddDistinctLine(lines, line);
        }

        foreach (var koreanLine in ReadLines(korean))
        {
            MergeKoreanLine(lines, koreanLine);
        }

        if (lines.Count == 0)
        {
            return !string.IsNullOrWhiteSpace(primary.Text) ? primary : korean;
        }

        var ordered = lines
            .OrderBy(line => line.BoundsPixels?.Y ?? int.MaxValue)
            .ThenBy(line => line.BoundsPixels?.X ?? int.MaxValue)
            .ThenBy(line => line.Text, StringComparer.Ordinal)
            .ToArray();

        return new OcrResult(
            string.Join("\n", ordered.Select(line => TextNormalizer.Normalize(line.Text))),
            EstimateTextHeightPixels(ordered),
            EstimateTextBoundsPixels(ordered),
            ordered);
    }

    private static IReadOnlyList<OcrTextLine> ReadLines(OcrResult result)
    {
        if (result.TextLines is { Count: > 0 })
        {
            return result.TextLines
                .Where(line => !string.IsNullOrWhiteSpace(line.Text))
                .ToArray();
        }

        return result.Text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => TextNormalizer.Normalize(line))
            .Where(line => line.Length > 0)
            .Select(line => new OcrTextLine(line))
            .ToArray();
    }

    private static void MergeKoreanLine(List<OcrTextLine> lines, OcrTextLine koreanLine)
    {
        var matchIndex = FindOverlappingLineIndex(lines, koreanLine);
        if (matchIndex < 0)
        {
            AddDistinctLine(lines, koreanLine);
            return;
        }

        var existing = lines[matchIndex];
        if (ShouldPreferKoreanLine(existing, koreanLine))
        {
            lines[matchIndex] = koreanLine;
        }
    }

    private static void AddDistinctLine(List<OcrTextLine> lines, OcrTextLine line)
    {
        var normalized = TextNormalizer.Normalize(line.Text);
        if (normalized.Length == 0 ||
            lines.Any(existing => string.Equals(
                TextNormalizer.Normalize(existing.Text),
                normalized,
                StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        lines.Add(line with { Text = normalized });
    }

    private static int FindOverlappingLineIndex(IReadOnlyList<OcrTextLine> lines, OcrTextLine candidate)
    {
        if (!candidate.BoundsPixels.HasValue)
        {
            return -1;
        }

        for (var index = 0; index < lines.Count; index++)
        {
            var existing = lines[index];
            if (existing.BoundsPixels.HasValue &&
                OverlapRatio(existing.BoundsPixels.Value, candidate.BoundsPixels.Value) >= OverlapReplacementThreshold)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool ShouldPreferKoreanLine(OcrTextLine existing, OcrTextLine koreanLine)
    {
        var koreanHasHangul = ContainsHangul(koreanLine.Text);
        if (!koreanHasHangul)
        {
            return false;
        }

        if (!ContainsHangul(existing.Text))
        {
            return true;
        }

        return UsefulCharacterCount(koreanLine.Text) > UsefulCharacterCount(existing.Text);
    }

    private static bool ContainsHangul(string text)
    {
        return text.Any(value => value is >= '\uAC00' and <= '\uD7AF');
    }

    private static int UsefulCharacterCount(string text)
    {
        return text.Count(char.IsLetterOrDigit);
    }

    private static double OverlapRatio(FrameRegion first, FrameRegion second)
    {
        var left = Math.Max(first.X, second.X);
        var top = Math.Max(first.Y, second.Y);
        var right = Math.Min(first.X + first.Width, second.X + second.Width);
        var bottom = Math.Min(first.Y + first.Height, second.Y + second.Height);
        var width = right - left;
        var height = bottom - top;
        if (width <= 0 || height <= 0)
        {
            return 0;
        }

        var intersectionArea = width * height;
        var smallerArea = Math.Min(first.Width * first.Height, second.Width * second.Height);
        return smallerArea <= 0 ? 0 : intersectionArea / (double)smallerArea;
    }

    private static double? EstimateTextHeightPixels(IReadOnlyList<OcrTextLine> lines)
    {
        var heights = lines
            .Select(line => line.BoundsPixels?.Height)
            .Where(height => height is > 0)
            .Select(height => (double)height!.Value)
            .Order()
            .ToArray();

        return heights.Length == 0 ? null : heights[heights.Length / 2];
    }

    private static FrameRegion? EstimateTextBoundsPixels(IReadOnlyList<OcrTextLine> lines)
    {
        var bounds = lines
            .Select(line => line.BoundsPixels)
            .Where(bounds => bounds.HasValue)
            .Select(bounds => bounds!.Value)
            .ToArray();

        if (bounds.Length == 0)
        {
            return null;
        }

        var left = bounds.Min(region => region.X);
        var top = bounds.Min(region => region.Y);
        var right = bounds.Max(region => region.X + region.Width);
        var bottom = bounds.Max(region => region.Y + region.Height);
        return new FrameRegion(
            left,
            top,
            Math.Max(1, right - left),
            Math.Max(1, bottom - top));
    }
}
