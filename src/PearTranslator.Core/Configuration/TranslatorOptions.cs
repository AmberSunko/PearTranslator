namespace PearTranslator.Core.Configuration;

public sealed class TranslatorOptions
{
    public int RequiredStableRepeats { get; init; } = 1;

    public TimeSpan SamplingInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    public bool ShowOcrPositionOnly { get; init; }

    public bool AlignTranslationToOcrLines { get; init; }

    public bool LocalPreviewEnabled { get; init; } = true;

    public TargetLanguage TargetLanguage { get; init; } = TargetLanguage.SimplifiedChinese;
}
