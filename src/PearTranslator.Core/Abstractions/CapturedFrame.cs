namespace PearTranslator.Core.Abstractions;

public sealed record CapturedFrame(
    FrameRegion Region,
    DateTimeOffset CapturedAt,
    byte[] Fingerprint,
    byte[] ImageBytes,
    string ImageMimeType,
    int ImageWidthPixels = 0,
    int ImageHeightPixels = 0)
{
    public const string RawBgra32MimeType = "application/x-bgra32";

    public CapturedFrame(FrameRegion region, DateTimeOffset capturedAt, byte[] fingerprint)
        : this(region, capturedAt, fingerprint, [], string.Empty)
    {
    }

    public bool HasImage => ImageBytes.Length > 0 && ImageMimeType.Length > 0;

    public bool HasRawBgra32Image => HasImage &&
        string.Equals(ImageMimeType, RawBgra32MimeType, StringComparison.Ordinal) &&
        ImageWidthPixels > 0 &&
        ImageHeightPixels > 0 &&
        ImageBytes.Length >= ImageWidthPixels * ImageHeightPixels * 4;
}
