namespace PearTranslator.Core.Pipeline;

public sealed record TranslationLoopResult(
    TranslationLoopOutcome Outcome,
    string ErrorMessage,
    TranslationTelemetry? Telemetry = null,
    string ConnectionStatusMessage = "")
{
    public static TranslationLoopResult NoChange { get; } = new(TranslationLoopOutcome.NoChange, string.Empty);

    public static TranslationLoopResult DisplayedTranslation { get; } = new(TranslationLoopOutcome.DisplayedTranslation, string.Empty);

    public static TranslationLoopResult Displayed(TranslationTelemetry telemetry)
    {
        return new TranslationLoopResult(TranslationLoopOutcome.DisplayedTranslation, string.Empty, telemetry);
    }

    public static TranslationLoopResult HiddenOverlay { get; } = new(TranslationLoopOutcome.HiddenOverlay, string.Empty);

    public static TranslationLoopResult SkippedNoEnglish { get; } = new(
        TranslationLoopOutcome.NoChange,
        string.Empty,
        ConnectionStatusMessage: "未检测到英文，未请求翻译服务");

    public static TranslationLoopResult Failed(Exception exception)
    {
        return new TranslationLoopResult(TranslationLoopOutcome.Failed, exception.Message);
    }
}
