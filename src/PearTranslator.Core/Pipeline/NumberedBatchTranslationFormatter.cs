using System.Text.Json;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Text;

namespace PearTranslator.Core.Pipeline;

public static class NumberedBatchTranslationFormatter
{
    public static string BuildPrompt(
        IReadOnlyList<string> lineTexts,
        TargetLanguage targetLanguage = TargetLanguage.SimplifiedChinese)
    {
        var payload = lineTexts
            .Select((line, index) => new { id = index + 1, text = line })
            .ToArray();

        return string.Join(
            '\n',
            BuildTranslationInstruction(targetLanguage),
            "Return only a JSON array. Each item must be {\"id\":number,\"text\":\"translation\"}.",
            "Keep every id exactly once. Do not merge, split, reorder, add notes, or add Markdown.",
            "Lines:",
            JsonSerializer.Serialize(payload));
    }

    private static string BuildTranslationInstruction(TargetLanguage targetLanguage)
    {
        return targetLanguage switch
        {
            TargetLanguage.English => "Translate each non-English game subtitle line into natural English.",
            _ => "Translate each non-Chinese game subtitle line into natural Simplified Chinese."
        };
    }

    public static bool TryParse(
        string translatedText,
        int expectedLineCount,
        out string[] translatedLines)
    {
        translatedLines = [];
        var json = ExtractJsonArray(translatedText);
        if (json is null)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var byId = new Dictionary<int, string>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !TryReadId(item, out var id) ||
                    !TryReadText(item, out var text) ||
                    id < 1 ||
                    id > expectedLineCount ||
                    byId.ContainsKey(id))
                {
                    return false;
                }

                byId[id] = NormalizeTranslatedLine(text);
            }

            if (byId.Count != expectedLineCount)
            {
                return false;
            }

            translatedLines = Enumerable.Range(1, expectedLineCount)
                .Select(id => byId[id])
                .ToArray();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string NormalizeTranslatedLine(string text)
    {
        return TextNormalizer.Normalize(TranslationTextNormalizer.NormalizeForDisplay(text));
    }

    private static string? ExtractJsonArray(string text)
    {
        var start = text.IndexOf('[', StringComparison.Ordinal);
        var end = text.LastIndexOf(']');
        if (start < 0 || end <= start)
        {
            return null;
        }

        return text[start..(end + 1)];
    }

    private static bool TryReadId(JsonElement item, out int id)
    {
        id = 0;
        if (!TryGetProperty(item, "id", out var value))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.TryGetInt32(out id);
        }

        return value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), out id);
    }

    private static bool TryReadText(JsonElement item, out string text)
    {
        text = string.Empty;
        if (!TryGetProperty(item, "text", out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        text = value.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(text);
    }

    private static bool TryGetProperty(JsonElement item, string name, out JsonElement value)
    {
        foreach (var property in item.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
