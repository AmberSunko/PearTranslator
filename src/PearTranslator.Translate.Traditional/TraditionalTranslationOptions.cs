using PearTranslator.Core.Configuration;

namespace PearTranslator.Translate.Traditional;

public sealed record TraditionalTranslationOptions(
    string ApiKey,
    Uri Endpoint,
    string Region = "",
    string Project = "",
    TargetLanguage TargetLanguage = TargetLanguage.SimplifiedChinese)
{
    public string ApiKey { get; } = RequireValue(ApiKey, nameof(ApiKey));

    public Uri Endpoint { get; } = Endpoint;

    public string Region { get; } = Region.Trim();

    public string Project { get; } = Project.Trim();

    public TargetLanguage TargetLanguage { get; } = TargetLanguage;

    private static string RequireValue(string value, string name)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be blank.", name)
            : value.Trim();
    }
}
