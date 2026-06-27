using PearTranslator.Core.Configuration;

namespace PearTranslator.Core.Pipeline;

public static class TranslationConnectionStatusFormatter
{
    public static string FormatNoRequest(TranslatorSettings settings, string reason)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason)
            ? "等待英文字幕"
            : reason.Trim();

        return settings.Translation.Provider switch
        {
            TranslationProviderKind.OpenAi =>
                $"{ServiceLabel(settings.Translation.OpenAi.Service)} · {normalizedReason}",
            TranslationProviderKind.Azure or TranslationProviderKind.DeepL or TranslationProviderKind.Google =>
                $"{settings.Translation.Provider}：{normalizedReason}",
            TranslationProviderKind.Mock =>
                $"模拟：{normalizedReason}",
            _ =>
                $"未接入 · {normalizedReason}"
        };
    }

    public static string Format(TranslatorSettings settings, TranslationTelemetry telemetry)
    {
        var latency = FormatLatency(telemetry.Latency);
        var providerLabel = string.IsNullOrWhiteSpace(telemetry.ProviderLabel)
            ? "未知"
            : telemetry.ProviderLabel.Trim();

        var status = settings.Translation.Provider switch
        {
            TranslationProviderKind.OpenAi when IsOcrPreview(providerLabel) =>
                $"{ServiceLabel(settings.Translation.OpenAi.Service)} 连接失败，显示 OCR 原文 · {latency}",
            TranslationProviderKind.OpenAi =>
                $"{ServiceLabel(settings.Translation.OpenAi.Service)} / {providerLabel} · 连接正常 · {latency}",
            TranslationProviderKind.Azure or TranslationProviderKind.DeepL or TranslationProviderKind.Google
                when IsOcrPreview(providerLabel) =>
                $"{settings.Translation.Provider}：连接失败，显示 OCR 原文 · {latency}",
            TranslationProviderKind.Azure or TranslationProviderKind.DeepL or TranslationProviderKind.Google =>
                $"{providerLabel}：连接正常 · {latency}",
            TranslationProviderKind.Mock =>
                $"模拟：本地 · {latency}",
            _ =>
                $"未接入 · OCR 预览 · {latency}"
        };

        return telemetry.Pipeline is null
            ? status
            : $"{status} · {FormatPipeline(telemetry.Pipeline, telemetry.RequestCount)}";
    }

    public static string FormatPreflightStarting(TranslatorSettings settings)
    {
        return $"{FormatProviderDisplayName(settings, providerLabel: string.Empty)} \u00b7 \u9884\u8fde\u63a5\u4e2d";
    }

    public static string FormatPreflightFailure(
        TranslatorSettings settings,
        TranslationConnectionProbeResult result)
    {
        var errorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
            ? "\u672a\u77e5\u9519\u8bef"
            : result.ErrorMessage.Trim();

        return string.Join(
            " \u00b7 ",
            FormatProviderDisplayName(settings, result.ProviderLabel),
            "\u8fde\u63a5\u5931\u8d25\uff0c\u663e\u793a OCR \u539f\u6587",
            FormatLatency(result.Latency),
            errorMessage);
    }

    private static bool IsOcrPreview(string providerLabel)
    {
        return string.Equals(providerLabel, "识别", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatLatency(TimeSpan latency)
    {
        var milliseconds = Math.Max(0, (int)Math.Round(latency.TotalMilliseconds));
        return $"{milliseconds} ms";
    }

    private static string FormatPipeline(TranslationPipelineTelemetry pipeline, int requestCount)
    {
        return string.Join(
            " / ",
            $"\u6700\u6162\uff1a{pipeline.SlowestStageName}",
            $"\u622a\u56fe {FormatLatency(pipeline.Capture)}",
            $"OCR {FormatLatency(pipeline.Ocr)}",
            $"\u7a33\u5b9a {FormatLatency(pipeline.Stabilization)}",
            $"\u7ffb\u8bd1 {FormatLatency(pipeline.Translation)}",
            $"\u663e\u793a {FormatLatency(pipeline.Overlay)}",
            $"\u8bf7\u6c42 {Math.Max(0, requestCount)}");
    }

    private static string FormatProviderDisplayName(TranslatorSettings settings, string providerLabel)
    {
        return settings.Translation.Provider switch
        {
            TranslationProviderKind.OpenAi =>
                $"{ServiceLabel(settings.Translation.OpenAi.Service)} / {ReadOpenAiModel(settings, providerLabel)}",
            TranslationProviderKind.Azure or TranslationProviderKind.DeepL or TranslationProviderKind.Google =>
                string.IsNullOrWhiteSpace(providerLabel)
                    ? settings.Translation.Provider.ToString()
                    : providerLabel.Trim(),
            TranslationProviderKind.Mock => "\u6a21\u62df",
            _ => "\u672a\u63a5\u5165"
        };
    }

    private static string ReadOpenAiModel(TranslatorSettings settings, string providerLabel)
    {
        return string.IsNullOrWhiteSpace(providerLabel)
            ? settings.Translation.OpenAi.EffectiveModel
            : providerLabel.Trim();
    }

    private static string ServiceLabel(OpenAiCompatibleService service)
    {
        return service switch
        {
            OpenAiCompatibleService.DeepSeek => "DeepSeek",
            OpenAiCompatibleService.Qwen => "通义千问 / Qwen",
            OpenAiCompatibleService.Kimi => "Moonshot / Kimi",
            OpenAiCompatibleService.Zhipu => "智谱 GLM",
            OpenAiCompatibleService.Doubao => "火山方舟 / Doubao",
            OpenAiCompatibleService.Custom => "自定义兼容接口",
            _ => "OpenAI"
        };
    }
}
