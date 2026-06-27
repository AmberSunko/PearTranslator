namespace PearTranslator.Core.Abstractions;

public interface IOverlayPresenter
{
    Task ShowAsync(string translatedText, CancellationToken cancellationToken);

    Task ShowAsync(string translatedText, string providerLabel, CancellationToken cancellationToken)
    {
        return ShowAsync(translatedText, cancellationToken);
    }

    Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        CancellationToken cancellationToken)
    {
        return ShowAsync(translatedText, providerLabel, cancellationToken);
    }

    Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        CancellationToken cancellationToken)
    {
        return ShowAsync(translatedText, providerLabel, sourceTextHeightPixels, cancellationToken);
    }

    Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        CancellationToken cancellationToken)
    {
        return ShowAsync(translatedText, providerLabel, sourceTextHeightPixels, anchorRegion, cancellationToken);
    }

    Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        string? sourceText,
        CancellationToken cancellationToken)
    {
        return ShowAsync(
            translatedText,
            providerLabel,
            sourceTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels,
            cancellationToken);
    }

    Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        string? sourceText,
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        CancellationToken cancellationToken)
    {
        return ShowAsync(
            translatedText,
            providerLabel,
            sourceTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels,
            sourceText,
            cancellationToken);
    }

    Task ShowOneShotAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        string? sourceText,
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        CancellationToken cancellationToken)
    {
        return ShowAsync(
            translatedText,
            providerLabel,
            sourceTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels,
            sourceText,
            sourceTextLines,
            cancellationToken);
    }

    Task HideAsync(CancellationToken cancellationToken);

    Task HideOneShotAsync(CancellationToken cancellationToken)
    {
        return HideAsync(cancellationToken);
    }
}
