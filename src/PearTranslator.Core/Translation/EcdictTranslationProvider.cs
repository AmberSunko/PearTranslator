using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Text;

namespace PearTranslator.Core.Translation;

public sealed class EcdictTranslationProvider : ITranslationProvider, ITranslationProviderMetadata
{
    private static readonly Regex WordPattern = new("[A-Za-z][A-Za-z'-]*", RegexOptions.Compiled);
    private readonly string _dictionaryPath;
    private readonly object _loadGate = new();
    private Task<IReadOnlyDictionary<string, string>>? _loadTask;

    public EcdictTranslationProvider(string dictionaryPath)
    {
        _dictionaryPath = dictionaryPath;
    }

    public string ProviderLabel => "本地";

    public async Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        var key = TextNormalizer.Normalize(sourceText).ToLowerInvariant();
        if (key.Length == 0)
        {
            throw new LocalDictionaryMissException(sourceText);
        }

        var entries = await LoadAsync(cancellationToken);
        if (entries.TryGetValue(key, out var translated))
        {
            return translated;
        }

        var punctuationMatchedTranslation = TryTranslateWithOuterPunctuation(entries, key);
        if (punctuationMatchedTranslation is not null)
        {
            return punctuationMatchedTranslation;
        }

        var wordTranslation = TryTranslateKnownWords(entries, key);
        if (wordTranslation is not null)
        {
            return wordTranslation;
        }

        throw new LocalDictionaryMissException(sourceText);
    }

    private static string? TryTranslateWithOuterPunctuation(
        IReadOnlyDictionary<string, string> entries,
        string sourceText)
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

        return entries.TryGetValue(body, out var translated)
            ? sourceText[..first] + translated + sourceText[(last + 1)..]
            : null;
    }

    private static string? TryTranslateKnownWords(
        IReadOnlyDictionary<string, string> entries,
        string sourceText)
    {
        var matches = WordPattern.Matches(sourceText);
        if (matches.Count == 0)
        {
            return null;
        }

        var replaced = 0;
        var cursor = 0;
        var translated = new StringBuilder(sourceText.Length);
        foreach (Match match in matches)
        {
            translated.Append(sourceText[cursor..match.Index]);
            if (entries.TryGetValue(match.Value.ToLowerInvariant(), out var wordTranslation))
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
        return replaced > 0 ? translated.ToString() : null;
    }

    private Task<IReadOnlyDictionary<string, string>> LoadAsync(CancellationToken cancellationToken)
    {
        lock (_loadGate)
        {
            _loadTask ??= Task.Run(LoadDictionary, cancellationToken);
            return _loadTask;
        }
    }

    private IReadOnlyDictionary<string, string> LoadDictionary()
    {
        if (!File.Exists(_dictionaryPath))
        {
            return new Dictionary<string, string>();
        }

        using var parser = new TextFieldParser(_dictionaryPath, Encoding.UTF8);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        var headers = parser.ReadFields();
        var wordIndex = Array.IndexOf(headers ?? [], "word");
        var translationIndex = Array.IndexOf(headers ?? [], "translation");
        if (wordIndex < 0 || translationIndex < 0)
        {
            return new Dictionary<string, string>();
        }

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        while (!parser.EndOfData)
        {
            var fields = ReadFields(parser);
            if (fields is null || fields.Length <= wordIndex || fields.Length <= translationIndex)
            {
                continue;
            }

            var key = TextNormalizer.Normalize(fields[wordIndex]).ToLowerInvariant();
            var translated = CleanTranslation(fields[translationIndex]);
            if (key.Length > 0 && translated.Length > 0)
            {
                entries.TryAdd(key, translated);
            }
        }

        return entries;
    }

    private static string[]? ReadFields(TextFieldParser parser)
    {
        try
        {
            return parser.ReadFields();
        }
        catch (MalformedLineException)
        {
            return null;
        }
    }

    private static string CleanTranslation(string value)
    {
        return TextNormalizer.Normalize(
            value
                .Replace("\\n", "；", StringComparison.Ordinal)
                .Replace("\r\n", "；", StringComparison.Ordinal)
                .Replace("\n", "；", StringComparison.Ordinal));
    }
}
