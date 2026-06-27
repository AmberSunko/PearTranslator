using PearTranslator.Core.Configuration;
using PearTranslator.Core.Pipeline;

namespace PearTranslator.Core.Tests.Pipeline;

public sealed class TranslationConnectionStatusFormatterTests
{
    [Fact]
    public void FormatShowsOpenAiCompatibleModelAsHealthyWithoutGenericModelPrefix()
    {
        var settings = CreateOpenAiSettings(OpenAiCompatibleService.DeepSeek);
        var telemetry = new TranslationTelemetry("deepseek-v4-flash", TimeSpan.FromMilliseconds(123.4));

        var status = TranslationConnectionStatusFormatter.Format(settings, telemetry);

        Assert.Equal("DeepSeek / deepseek-v4-flash · 连接正常 · 123 ms", status);
    }

    [Fact]
    public void FormatShowsRemoteFailureWithoutGenericModelPrefixWhenOpenAiCompatibleProviderFallsBackToOcrPreview()
    {
        var settings = CreateOpenAiSettings(OpenAiCompatibleService.Qwen);
        var telemetry = new TranslationTelemetry("识别", TimeSpan.FromMilliseconds(980.7));

        var status = TranslationConnectionStatusFormatter.Format(settings, telemetry);

        Assert.Equal("通义千问 / Qwen 连接失败，显示 OCR 原文 · 981 ms", status);
    }

    [Fact]
    public void FormatNoRequestShowsOpenAiCompatibleServiceAndReasonWithoutGenericModelPrefix()
    {
        var settings = CreateOpenAiSettings(OpenAiCompatibleService.DeepSeek);

        var status = TranslationConnectionStatusFormatter.FormatNoRequest(
            settings,
            "未检测到英文，未请求翻译服务");

        Assert.Equal("DeepSeek · 未检测到英文，未请求翻译服务", status);
    }

    [Fact]
    public void FormatPreflightStartingShowsSelectedServiceAndModel()
    {
        var settings = CreateOpenAiSettings(OpenAiCompatibleService.DeepSeek);

        var status = TranslationConnectionStatusFormatter.FormatPreflightStarting(settings);

        Assert.Equal("DeepSeek / deepseek-chat \u00b7 \u9884\u8fde\u63a5\u4e2d", status);
    }

    [Fact]
    public void FormatPreflightFailureShowsLatencyAndReason()
    {
        var settings = CreateOpenAiSettings(OpenAiCompatibleService.DeepSeek);
        var result = new TranslationConnectionProbeResult(
            Succeeded: false,
            ProviderLabel: "deepseek-chat",
            Latency: TimeSpan.FromMilliseconds(432.1),
            ErrorMessage: "401 Unauthorized");

        var status = TranslationConnectionStatusFormatter.FormatPreflightFailure(settings, result);

        Assert.Equal(
            "DeepSeek / deepseek-chat \u00b7 \u8fde\u63a5\u5931\u8d25\uff0c\u663e\u793a OCR \u539f\u6587 \u00b7 432 ms \u00b7 401 Unauthorized",
            status);
    }

    [Fact]
    public void FormatAppendsPipelineBreakdownWhenTelemetryContainsPerformanceData()
    {
        var settings = CreateOpenAiSettings(OpenAiCompatibleService.DeepSeek);
        var telemetry = new TranslationTelemetry(
            "deepseek-chat",
            TimeSpan.FromMilliseconds(900),
            RequestCount: 4,
            Pipeline: new TranslationPipelineTelemetry(
                Capture: TimeSpan.FromMilliseconds(12),
                Ocr: TimeSpan.FromMilliseconds(88),
                Stabilization: TimeSpan.FromMilliseconds(501),
                Translation: TimeSpan.FromMilliseconds(900),
                Overlay: TimeSpan.FromMilliseconds(3)));

        var status = TranslationConnectionStatusFormatter.Format(settings, telemetry);

        Assert.Contains("\u6700\u6162\uff1a\u7ffb\u8bd1", status);
        Assert.Contains("\u622a\u56fe 12 ms", status);
        Assert.Contains("OCR 88 ms", status);
        Assert.Contains("\u7a33\u5b9a 501 ms", status);
        Assert.Contains("\u7ffb\u8bd1 900 ms", status);
        Assert.Contains("\u663e\u793a 3 ms", status);
        Assert.Contains("\u8bf7\u6c42 4", status);
    }

    [Theory]
    [InlineData(0, TranslationLatencyBand.Fast)]
    [InlineData(199, TranslationLatencyBand.Fast)]
    [InlineData(200, TranslationLatencyBand.Normal)]
    [InlineData(399, TranslationLatencyBand.Normal)]
    [InlineData(400, TranslationLatencyBand.Slow)]
    [InlineData(801, TranslationLatencyBand.Slow)]
    public void ClassifyLatencyUsesUiThresholds(int milliseconds, TranslationLatencyBand expected)
    {
        Assert.Equal(expected, TranslationLatencyClassifier.Classify(TimeSpan.FromMilliseconds(milliseconds)));
    }

    private static TranslatorSettings CreateOpenAiSettings(OpenAiCompatibleService service)
    {
        return new TranslatorSettings
        {
            Translation = new TranslationSettings
            {
                Provider = TranslationProviderKind.OpenAi,
                OpenAi = new OpenAiProviderSettings
                {
                    Service = service,
                    ApiKey = "test-key",
                    Model = OpenAiProviderSettings.GetDefaultModel(service)
                }
            }
        };
    }
}
