namespace PearTranslator.Core.Abstractions;

public sealed record OcrTextLine(string Text, FrameRegion? BoundsPixels = null);

public sealed record OcrResult(
    string Text,
    double? EstimatedTextHeightPixels = null,
    FrameRegion? TextBoundsPixels = null,
    IReadOnlyList<OcrTextLine>? TextLines = null);
