using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using CoreOcrResult = PearTranslator.Core.Abstractions.OcrResult;
using WindowsOcrResult = Windows.Media.Ocr.OcrResult;

namespace PearTranslator.Ocr.Windows;

public sealed class WindowsOcrEngine : IOcrEngine
{
    private readonly OcrEngine _engine;

    private WindowsOcrEngine(OcrEngine engine)
    {
        _engine = engine;
    }

    public static WindowsOcrEngine? TryCreate(string languageTag = "en-US")
    {
        var language = new Language(languageTag);
        var engine = OcrEngine.TryCreateFromLanguage(language)
            ?? OcrEngine.TryCreateFromUserProfileLanguages();

        return engine is null ? null : new WindowsOcrEngine(engine);
    }

    public static WindowsOcrEngine? TryCreateFromUserProfileLanguages()
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages();
        return engine is null ? null : new WindowsOcrEngine(engine);
    }

    public async Task<CoreOcrResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
    {
        if (!frame.HasImage)
        {
            return new CoreOcrResult(string.Empty);
        }

        using var bitmap = frame.HasRawBgra32Image
            ? CreateRawSoftwareBitmap(frame)
            : await DecodeSoftwareBitmapAsync(frame, cancellationToken);

        var result = await _engine.RecognizeAsync(bitmap).AsTask(cancellationToken);
        var geometry = OcrGeometryNormalizer.Normalize(
            frame.Region,
            bitmap.PixelWidth,
            bitmap.PixelHeight,
            EstimateTextBoundsPixels(result),
            EstimateTextHeightPixels(result),
            EstimateTextLines(result));

        return new CoreOcrResult(
            string.Join("\n", result.Lines.Select(line => line.Text)),
            geometry.EstimatedTextHeightPixels,
            geometry.TextBoundsPixels,
            geometry.TextLines);
    }

    private static SoftwareBitmap CreateRawSoftwareBitmap(CapturedFrame frame)
    {
        var writer = new DataWriter();
        try
        {
            writer.WriteBytes(frame.ImageBytes);
            var buffer = writer.DetachBuffer();
            return SoftwareBitmap.CreateCopyFromBuffer(
                buffer,
                BitmapPixelFormat.Bgra8,
                frame.ImageWidthPixels,
                frame.ImageHeightPixels,
                BitmapAlphaMode.Premultiplied);
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static async Task<SoftwareBitmap> DecodeSoftwareBitmapAsync(
        CapturedFrame frame,
        CancellationToken cancellationToken)
    {
        using var stream = await OpenImageStreamAsync(frame.ImageBytes, cancellationToken);
        var decoder = await BitmapDecoder.CreateAsync(stream).AsTask(cancellationToken);
        return await decoder
            .GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied)
            .AsTask(cancellationToken);
    }

    private static double? EstimateTextHeightPixels(WindowsOcrResult result)
    {
        var heights = result.Lines
            .SelectMany(line => line.Words)
            .Select(word => word.BoundingRect.Height)
            .Where(height => height > 0)
            .Order()
            .ToArray();

        if (heights.Length == 0)
        {
            return null;
        }

        return heights[heights.Length / 2];
    }

    private static FrameRegion? EstimateTextBoundsPixels(WindowsOcrResult result)
    {
        var bounds = result.Lines
            .SelectMany(line => line.Words)
            .Select(word => word.BoundingRect)
            .Where(rect => rect.Width > 0 && rect.Height > 0)
            .ToArray();

        if (bounds.Length == 0)
        {
            return null;
        }

        var left = bounds.Min(rect => rect.X);
        var top = bounds.Min(rect => rect.Y);
        var right = bounds.Max(rect => rect.X + rect.Width);
        var bottom = bounds.Max(rect => rect.Y + rect.Height);

        return new FrameRegion(
            (int)Math.Round(left),
            (int)Math.Round(top),
            Math.Max(1, (int)Math.Round(right - left)),
            Math.Max(1, (int)Math.Round(bottom - top)));
    }

    private static IReadOnlyList<OcrTextLine> EstimateTextLines(WindowsOcrResult result)
    {
        return result.Lines
            .Select(line => new OcrTextLine(line.Text, EstimateTextBoundsPixels(line.Words)))
            .ToArray();
    }

    private static FrameRegion? EstimateTextBoundsPixels(IReadOnlyList<OcrWord> words)
    {
        var bounds = words
            .Select(word => word.BoundingRect)
            .Where(rect => rect.Width > 0 && rect.Height > 0)
            .ToArray();

        if (bounds.Length == 0)
        {
            return null;
        }

        var left = bounds.Min(rect => rect.X);
        var top = bounds.Min(rect => rect.Y);
        var right = bounds.Max(rect => rect.X + rect.Width);
        var bottom = bounds.Max(rect => rect.Y + rect.Height);

        return new FrameRegion(
            (int)Math.Round(left),
            (int)Math.Round(top),
            Math.Max(1, (int)Math.Round(right - left)),
            Math.Max(1, (int)Math.Round(bottom - top)));
    }

    private static async Task<InMemoryRandomAccessStream> OpenImageStreamAsync(
        byte[] imageBytes,
        CancellationToken cancellationToken)
    {
        var stream = new InMemoryRandomAccessStream();
        var writer = new DataWriter(stream);

        try
        {
            writer.WriteBytes(imageBytes);
            await writer.StoreAsync().AsTask(cancellationToken);
            await writer.FlushAsync().AsTask(cancellationToken);
            writer.DetachStream();
            stream.Seek(0);
            return stream;
        }
        finally
        {
            writer.Dispose();
        }
    }
}
