using System.Net.Http.Headers;
using System.Text.Json;

namespace PearTranslator.Translate.OpenAI;

public sealed class OpenAiCompatibleModelCatalogClient
{
    private readonly HttpClient _httpClient;

    public OpenAiCompatibleModelCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<string>> ListModelsAsync(
        OpenAiTranslationOptions options,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(options.BaseUri, "models"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new OpenAiTranslationException(
                $"Model list request failed with {(int)response.StatusCode} {response.StatusCode}: {ExtractErrorMessage(responseBody)}",
                response.StatusCode);
        }

        return ExtractModelIds(responseBody);
    }

    private static IReadOnlyList<string> ExtractModelIds(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind is not JsonValueKind.Array)
            {
                return [];
            }

            return data
                .EnumerateArray()
                .Select(ReadModelId)
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Select(model => model!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException exception)
        {
            throw new OpenAiTranslationException("Model list response was not valid JSON.", innerException: exception);
        }
    }

    private static string? ReadModelId(JsonElement model)
    {
        if (model.ValueKind == JsonValueKind.String)
        {
            return model.GetString();
        }

        return model.ValueKind == JsonValueKind.Object &&
            model.TryGetProperty("id", out var id) &&
            id.ValueKind == JsonValueKind.String
            ? id.GetString()
            : null;
    }

    private static string ExtractErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return "empty response body";
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            return root.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message)
                ? message.GetString() ?? responseBody
                : responseBody;
        }
        catch (JsonException)
        {
            return responseBody;
        }
    }
}
