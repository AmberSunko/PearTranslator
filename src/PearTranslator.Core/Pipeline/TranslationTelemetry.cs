namespace PearTranslator.Core.Pipeline;

public sealed record TranslationTelemetry(
    string ProviderLabel,
    TimeSpan Latency,
    int RequestCount = 1,
    TranslationPipelineTelemetry? Pipeline = null);

public sealed record TranslationPipelineTelemetry(
    TimeSpan Capture,
    TimeSpan Ocr,
    TimeSpan Stabilization,
    TimeSpan Translation,
    TimeSpan Overlay)
{
    public string SlowestStageName
    {
        get
        {
            var stages = new[]
            {
                new Stage("\u622a\u56fe", Capture),
                new Stage("OCR", Ocr),
                new Stage("\u7a33\u5b9a", Stabilization),
                new Stage("\u7ffb\u8bd1", Translation),
                new Stage("\u663e\u793a", Overlay)
            };

            return stages
                .OrderByDescending(stage => stage.Duration)
                .First()
                .Name;
        }
    }

    private sealed record Stage(string Name, TimeSpan Duration);
}
