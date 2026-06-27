using System.Net;
using System.Text.Json;
using PearTranslator.Core.Abstractions;

namespace PearTranslator.Translate.Traditional;

public abstract class TraditionalTranslationProviderBase : ITranslationProvider
{
    protected static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    protected readonly HttpClient HttpClient;
    protected readonly TraditionalTranslationOptions Options;

    protected TraditionalTranslationProviderBase(HttpClient httpClient, TraditionalTranslationOptions options)
    {
        HttpClient = httpClient;
        Options = options;
    }

    public abstract Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken);

    protected static async Task<string> ReadSuccessBodyOrThrowAsync(
        HttpResponseMessage response,
        string providerName,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return body;
        }

        throw new TraditionalTranslationException(
            $"{providerName} translation failed with {(int)response.StatusCode} {response.StatusCode}: {ExtractErrorMessage(body)}",
            response.StatusCode);
    }

    protected static string RequireTranslation(string providerName, string? translatedText)
    {
        if (string.IsNullOrWhiteSpace(translatedText))
        {
            throw new TraditionalTranslationException($"{providerName} returned no translation content.");
        }

        return WebUtility.HtmlDecode(translatedText.Trim());
    }

    protected static Uri AppendPath(Uri endpoint, string relativePath)
    {
        var baseUri = endpoint.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? endpoint
            : new Uri(endpoint.AbsoluteUri + "/");
        return new Uri(baseUri, relativePath);
    }

    protected static Uri AppendQuery(Uri uri, IReadOnlyDictionary<string, string> values)
    {
        var query = string.Join("&", values.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
        return new Uri(uri.AbsoluteUri + separator + query);
    }

    private static string ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "empty response body";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? body;
            }

            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString() ?? body;
                }

                if (error.TryGetProperty("message", out var errorMessage))
                {
                    return errorMessage.GetString() ?? body;
                }
            }
        }
        catch (JsonException)
        {
        }

        return body;
    }
}
