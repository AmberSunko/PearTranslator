using System.Text.RegularExpressions;
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Text;

namespace PearTranslator.Core.Translation;

public sealed partial class FirstWordLocalPreviewProvider : ITranslationProviderMetadata
{
    private const string EmptyLineMarker = "\u200B";
    private const int MaxPreviewCacheEntries = 128;
    private readonly ITranslationProvider _dictionaryProvider;
    private readonly object _previewCacheGate = new();
    private readonly Dictionary<string, string?> _previewCache = new(StringComparer.Ordinal);
    private readonly Queue<string> _previewCacheKeys = new();

    public FirstWordLocalPreviewProvider(ITranslationProvider dictionaryProvider)
    {
        _dictionaryProvider = dictionaryProvider;
    }

    public string ProviderLabel => "local";

    public async Task<string?> BuildPreviewAsync(
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        string fallbackSourceText,
        CancellationToken cancellationToken)
    {
        var lineTexts = BuildPreviewLineTexts(sourceTextLines, fallbackSourceText);
        if (lineTexts.Length == 0)
        {
            return null;
        }

        var cacheKey = string.Join('\n', lineTexts);
        if (TryReadPreviewCache(cacheKey, out var cachedPreview))
        {
            return cachedPreview;
        }

        var hasPreview = false;
        var previewLines = new string[lineTexts.Length];
        for (var index = 0; index < lineTexts.Length; index++)
        {
            var preview = await TryPreviewLineAsync(lineTexts[index], cancellationToken);
            previewLines[index] = preview ?? EmptyLineMarker;
            hasPreview |= preview is not null;
        }

        var result = hasPreview ? string.Join('\n', previewLines) : null;
        StorePreviewCache(cacheKey, result);
        return result;
    }

    public async Task WarmUpAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dictionaryProvider.TranslateAsync("the", cancellationToken);
        }
        catch (LocalDictionaryMissException)
        {
        }
    }

    private async Task<string?> TryPreviewLineAsync(string sourceText, CancellationToken cancellationToken)
    {
        var match = FirstWordPattern().Match(sourceText);
        if (!match.Success)
        {
            return null;
        }

        try
        {
            var translated = await _dictionaryProvider.TranslateAsync(match.Value, cancellationToken);
            var preview = CleanPreviewTranslation(translated);
            return preview.Length > 0 ? preview : null;
        }
        catch (LocalDictionaryMissException)
        {
            return null;
        }
    }

    private bool TryReadPreviewCache(string key, out string? preview)
    {
        lock (_previewCacheGate)
        {
            return _previewCache.TryGetValue(key, out preview);
        }
    }

    private void StorePreviewCache(string key, string? preview)
    {
        lock (_previewCacheGate)
        {
            if (_previewCache.ContainsKey(key))
            {
                _previewCache[key] = preview;
                return;
            }

            _previewCache[key] = preview;
            _previewCacheKeys.Enqueue(key);

            while (_previewCacheKeys.Count > MaxPreviewCacheEntries)
            {
                _previewCache.Remove(_previewCacheKeys.Dequeue());
            }
        }
    }

    private static string CleanPreviewTranslation(string text)
    {
        var normalized = TextNormalizer.Normalize(text);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        normalized = RemoveDictionaryMetadata(normalized);
        var separatorIndex = normalized.IndexOfAny([',', ';', '\uFF0C', '\uFF1B', '\n']);
        if (separatorIndex >= 0)
        {
            normalized = normalized[..separatorIndex].Trim();
        }

        normalized = RemoveDictionaryMetadata(normalized);
        return normalized.Length > 0 && !IsInflectionOnlyPreview(normalized) ? normalized : string.Empty;
    }

    private static bool IsInflectionOnlyPreview(string text)
    {
        return InflectionOnlyPreviewPattern().IsMatch(text);
    }

    private static string RemoveDictionaryMetadata(string text)
    {
        var cleaned = ParenthesizedMetadataPattern().Replace(text, string.Empty);
        string previous;
        do
        {
            previous = cleaned;
            cleaned = LeadingMetadataPattern().Replace(cleaned, string.Empty).Trim();
        }
        while (!string.Equals(previous, cleaned, StringComparison.Ordinal));

        return RepeatedWhitespacePattern().Replace(cleaned, " ").Trim();
    }

    private static string[] BuildPreviewLineTexts(
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        string fallbackSourceText)
    {
        var boundedLines = OcrLineTextExtractor.GetTranslatableBoundedLineTexts(sourceTextLines);

        if (boundedLines.Length > 0)
        {
            return boundedLines;
        }

        return OcrLineTextExtractor.GetTranslatableFallbackLineTexts(fallbackSourceText);
    }

    [GeneratedRegex(@"[A-Za-z][A-Za-z'-]*")]
    private static partial Regex FirstWordPattern();

    [GeneratedRegex(@"^(?:(?:n|v|vi|vt|a|s|r|adj|adv|prep|conj|pron|abbr|int|interj|num|art|aux|suf|pref|pl)\.\s*|comb\.form\s*|stuff\.\s*|[\[\u3010][^\]\u3011]*[\]\u3011]\s*|[\(\uFF08][^\)\uFF09]*[\)\uFF09]\s*)", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingMetadataPattern();

    [GeneratedRegex(@"[\[\u3010][^\]\u3011]*[\]\u3011]|[\(\uFF08][^\)\uFF09]*[\)\uFF09]")]
    private static partial Regex ParenthesizedMetadataPattern();

    [GeneratedRegex(@"^(?:[A-Za-z][A-Za-z'-]*\s*)?(?:\u7684\s*)?(?:\u7b2c[一二三1-3]\u4eba\u79f0(?:\u5355\u6570)?|\u8fc7\u53bb\u5f0f|\u8fc7\u53bb\u5206\u8bcd|\u73b0\u5728\u5206\u8bcd|ing\u5f62\u5f0f|\u590d\u6570|\u53d8\u5f62)(?:\s*(?:\u548c|\u6216|\u53ca|\u4e0e|/|,|\uFF0C|;|\uFF1B)\s*(?:\u7b2c[一二三1-3]\u4eba\u79f0(?:\u5355\u6570)?|\u8fc7\u53bb\u5f0f|\u8fc7\u53bb\u5206\u8bcd|\u73b0\u5728\u5206\u8bcd|ing\u5f62\u5f0f|\u590d\u6570|\u53d8\u5f62))*$|^(?:plural|past tense|past participle|present participle|third-person singular)\s+of\s+[A-Za-z][A-Za-z'-]*$", RegexOptions.IgnoreCase)]
    private static partial Regex InflectionOnlyPreviewPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex RepeatedWhitespacePattern();
}
