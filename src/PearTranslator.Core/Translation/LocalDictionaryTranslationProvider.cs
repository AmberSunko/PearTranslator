using System.Text.RegularExpressions;
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Text;

namespace PearTranslator.Core.Translation;

public sealed partial class LocalDictionaryTranslationProvider : ITranslationProvider, ITranslationProviderMetadata
{
    private readonly IReadOnlyDictionary<string, string> _entries;

    public LocalDictionaryTranslationProvider(IReadOnlyDictionary<string, string> entries)
    {
        _entries = entries.ToDictionary(
            pair => TextNormalizer.Normalize(pair.Key).ToLowerInvariant(),
            pair => pair.Value,
            StringComparer.Ordinal);
    }

    public string ProviderLabel => "本地";

    public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        var normalized = TextNormalizer.Normalize(sourceText);
        var key = normalized.ToLowerInvariant();
        if (_entries.TryGetValue(key, out var exactTranslation))
        {
            return Task.FromResult(exactTranslation);
        }

        var punctuationMatchedTranslation = TryTranslateWithOuterPunctuation(normalized);
        if (punctuationMatchedTranslation is not null)
        {
            return Task.FromResult(punctuationMatchedTranslation);
        }

        foreach (var entry in _entries)
        {
            var translated = TryApplyTemplate(entry.Key, entry.Value, normalized);
            if (translated is not null)
            {
                return Task.FromResult(translated);
            }
        }

        var wordTranslation = TryTranslateKnownWords(normalized);
        if (wordTranslation is not null)
        {
            return Task.FromResult(wordTranslation);
        }

        throw new LocalDictionaryMissException(sourceText);
    }

    private string? TryTranslateWithOuterPunctuation(string sourceText)
    {
        var first = 0;
        while (first < sourceText.Length && char.IsPunctuation(sourceText[first]))
        {
            first++;
        }

        var last = sourceText.Length - 1;
        while (last >= first && char.IsPunctuation(sourceText[last]))
        {
            last--;
        }

        if (first == 0 && last == sourceText.Length - 1)
        {
            return null;
        }

        var body = sourceText[first..(last + 1)].Trim();
        if (body.Length == 0)
        {
            return null;
        }

        return _entries.TryGetValue(body.ToLowerInvariant(), out var translated)
            ? sourceText[..first] + translated + sourceText[(last + 1)..]
            : null;
    }

    private string? TryTranslateKnownWords(string sourceText)
    {
        var matches = WordPattern().Matches(sourceText);
        if (matches.Count == 0)
        {
            return null;
        }

        var replaced = 0;
        var cursor = 0;
        var translated = new System.Text.StringBuilder(sourceText.Length);
        foreach (Match match in matches)
        {
            translated.Append(sourceText[cursor..match.Index]);
            var key = match.Value.ToLowerInvariant();
            if (_entries.TryGetValue(key, out var wordTranslation))
            {
                translated.Append(wordTranslation);
                replaced++;
            }
            else
            {
                translated.Append(match.Value);
            }

            cursor = match.Index + match.Length;
        }

        translated.Append(sourceText[cursor..]);
        return replaced > 0 ? CleanTokenTranslation(translated.ToString()) : null;
    }

    private static string CleanTokenTranslation(string text)
    {
        return SpaceBeforePunctuationPattern()
            .Replace(RepeatedWhitespacePattern().Replace(text, " ").Trim(), "$1");
    }

    private static string? TryApplyTemplate(string template, string translationTemplate, string sourceText)
    {
        var placeholderMatch = PlaceholderPattern().Match(template);
        if (!placeholderMatch.Success)
        {
            return null;
        }

        var placeholder = placeholderMatch.Groups["name"].Value;
        var before = template[..placeholderMatch.Index];
        var after = template[(placeholderMatch.Index + placeholderMatch.Length)..];
        var escaped = Regex.Escape(before) + $"(?<{placeholder}>.+)" + Regex.Escape(after);
        var match = Regex.Match(sourceText, $"^{escaped}$", RegexOptions.IgnoreCase);
        return match.Success
            ? translationTemplate.Replace("{" + placeholder + "}", match.Groups[placeholder].Value, StringComparison.Ordinal)
            : null;
    }

    [GeneratedRegex(@"\{(?<name>[A-Za-z][A-Za-z0-9_]*)\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"[A-Za-z][A-Za-z'-]*")]
    private static partial Regex WordPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex RepeatedWhitespacePattern();

    [GeneratedRegex(@"\s+([:;,.!?])")]
    private static partial Regex SpaceBeforePunctuationPattern();
}
