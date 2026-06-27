using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;

namespace PearTranslator.Translate.OpenAI;

public sealed class OpenAiTranslationProvider : ITranslationProvider
    , ITranslationProviderMetadata
{
    private const string EnglishToChinesePrompt =
        "Translate English game subtitles into natural Simplified Chinese. Preserve the input line breaks and line count. Return only the translation without quotes, notes, or explanations.";
    private const string NonEnglishToEnglishPrompt =
        "Translate non-English game subtitles into natural English. Preserve the input line breaks and line count. Return only the translation without quotes, notes, or explanations.";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly OpenAiTranslationOptions _options;

    public OpenAiTranslationProvider(HttpClient httpClient, OpenAiTranslationOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public string ProviderLabel => _options.Model;

    public async Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return string.Empty;
        }

        using var request = BuildRequest(sourceText);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new OpenAiTranslationException(
                $"OpenAI translation failed with {(int)response.StatusCode} {response.StatusCode}: {ExtractErrorMessage(responseBody)}",
                response.StatusCode);
        }

        var translation = ExtractTranslation(responseBody);
        if (string.IsNullOrWhiteSpace(translation))
        {
            throw new OpenAiTranslationException("No translation content was returned by OpenAI.");
        }

        return translation.Trim();
    }

    private HttpRequestMessage BuildRequest(string sourceText)
    {
        var payload = IsQwenMtModel(_options.Model)
            ? BuildQwenMtPayload(sourceText)
            : BuildGeneralPayload(sourceText);

        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUri, "chat/completions"))
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return request;
    }

    private object BuildGeneralPayload(string sourceText)
    {
        if (IsDeepSeekV4Model(_options.Model))
        {
            return new
            {
                model = _options.Model,
                messages = new[]
                {
                    new { role = "system", content = DeveloperPrompt },
                    new { role = "user", content = sourceText }
                },
                thinking = new
                {
                    type = "disabled"
                }
            };
        }

        return new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = DeveloperPrompt },
                new { role = "user", content = sourceText }
            }
        };
    }

    private object BuildQwenMtPayload(string sourceText)
    {
        return new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "user", content = sourceText }
            },
            translation_options = new
            {
                source_lang = "auto",
                target_lang = ToQwenTargetLanguage(_options.TargetLanguage)
            }
        };
    }

    private string DeveloperPrompt => _options.TargetLanguage switch
    {
        TargetLanguage.English => NonEnglishToEnglishPrompt,
        _ => EnglishToChinesePrompt
    };

    private static string ToQwenTargetLanguage(TargetLanguage targetLanguage)
    {
        return targetLanguage switch
        {
            TargetLanguage.English => "English",
            _ => "Chinese"
        };
    }

    private static bool IsQwenMtModel(string model)
    {
        return model.StartsWith("qwen-mt-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDeepSeekV4Model(string model)
    {
        return model.StartsWith("deepseek-v4-", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractTranslation(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var message = choices[0].GetProperty("message");
            return message.TryGetProperty("content", out var content)
                ? content.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (JsonException exception)
        {
            throw new OpenAiTranslationException("OpenAI returned an invalid JSON response.", innerException: exception);
        }
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
