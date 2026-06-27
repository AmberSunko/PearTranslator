using System.Text.Json;
using System.Text.Json.Serialization;

namespace PearTranslator.Core.Configuration;

public sealed class TranslatorSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;

    public TranslatorSettingsStore(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public static TranslatorSettingsStore CreateDefault()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new TranslatorSettingsStore(Path.Combine(appData, "PearTranslator", "settings.json"));
    }

    public TranslatorSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new TranslatorSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<TranslatorSettings>(json, JsonOptions) ?? new TranslatorSettings();
        }
        catch (JsonException)
        {
            return new TranslatorSettings();
        }
        catch (IOException)
        {
            return new TranslatorSettings();
        }
    }

    public void Save(TranslatorSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }
}
