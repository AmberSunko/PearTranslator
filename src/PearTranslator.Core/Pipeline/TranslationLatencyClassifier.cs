namespace PearTranslator.Core.Pipeline;

public enum TranslationLatencyBand
{
    Fast,
    Normal,
    Slow
}

public static class TranslationLatencyClassifier
{
    public static TranslationLatencyBand Classify(TimeSpan latency)
    {
        var milliseconds = Math.Max(0, latency.TotalMilliseconds);
        if (milliseconds < 200)
        {
            return TranslationLatencyBand.Fast;
        }

        return milliseconds < 400
            ? TranslationLatencyBand.Normal
            : TranslationLatencyBand.Slow;
    }
}
