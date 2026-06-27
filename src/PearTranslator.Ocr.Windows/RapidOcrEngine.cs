using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Assets;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Text;
using RapidOcrNet;
using SkiaSharp;
using System.Runtime.InteropServices;
using CoreOcrResult = PearTranslator.Core.Abstractions.OcrResult;

namespace PearTranslator.Ocr.Windows;

public sealed class RapidOcrEngine : IOcrEngine, IDisposable
{
    private readonly RapidOcr _ocr;
    private readonly RapidOcrOptions _options;
    private readonly object _syncRoot = new();

    private RapidOcrEngine(RapidOcr ocr)
    {
        _ocr = ocr;
        _options = RapidOcrOptions.Default with
        {
            DoAngle = false,
            ReturnWordBox = false,
            TextScore = 0.5f
        };
    }

    public static bool TryCreate(OcrLanguageKind language, out RapidOcrEngine engine)
    {
        try
        {
            var ocr = new RapidOcr();
            using var sessionOptions = RapidOcr.GetDefaultSessionOptions();
            if (TryResolveExistingModelPaths(language, out var modelPaths))
            {
                ocr.InitModels(
                    modelPaths.DetPath,
                    modelPaths.ClsPath,
                    modelPaths.RecPath,
                    modelPaths.KeysPath,
                    sessionOptions);
            }
            else
            {
                ocr.InitModels(sessionOptions);
            }

            engine = new RapidOcrEngine(ocr);
            return true;
        }
        catch
        {
            engine = null!;
            return false;
        }
    }

    internal static ModelPaths ResolveModelPaths(OcrLanguageKind language)
    {
        return ResolveModelPaths(language, RuntimeAssetLocator.DefaultUserAssetRootDirectory, AppContext.BaseDirectory);
    }

    internal static ModelPaths ResolveModelPaths(
        OcrLanguageKind language,
        string userAssetRootDirectory,
        string applicationRootDirectory)
    {
        var clsPath = ResolveRuntimeAsset(
            "models/v5/ch_ppocr_mobile_v2.0_cls_infer.onnx",
            userAssetRootDirectory,
            applicationRootDirectory);

        if (UsesPpOcrV6Small(language))
        {
            return new ModelPaths(
                ResolveRuntimeAsset(
                    "models/v6/PP-OCRv6_small_det.onnx",
                    userAssetRootDirectory,
                    applicationRootDirectory),
                clsPath,
                ResolveRuntimeAsset(
                    "models/v6/PP-OCRv6_small_rec.onnx",
                    userAssetRootDirectory,
                    applicationRootDirectory),
                ResolveRuntimeAsset(
                    "models/v6/ppocrv6_dict.txt",
                    userAssetRootDirectory,
                    applicationRootDirectory));
        }

        return new ModelPaths(
            ResolveRuntimeAsset(
                "models/v5/ch_PP-OCRv5_mobile_det.onnx",
                userAssetRootDirectory,
                applicationRootDirectory),
            clsPath,
            ResolveRuntimeAsset(
                $"models/v5/{GetRecognizerModelFileName(language)}",
                userAssetRootDirectory,
                applicationRootDirectory),
            ResolveRuntimeAsset(
                $"models/v5/{GetDictionaryFileName(language)}",
                userAssetRootDirectory,
                applicationRootDirectory));
    }

    private static string ResolveRuntimeAsset(
        string relativePath,
        string userAssetRootDirectory,
        string applicationRootDirectory)
    {
        return RuntimeAssetLocator.ResolvePath(relativePath, userAssetRootDirectory, applicationRootDirectory);
    }

    private static bool UsesPpOcrV6Small(OcrLanguageKind language)
    {
        return language is OcrLanguageKind.Auto or OcrLanguageKind.English or OcrLanguageKind.Chinese or OcrLanguageKind.Japanese;
    }

    private static bool TryResolveExistingModelPaths(OcrLanguageKind language, out ModelPaths modelPaths)
    {
        modelPaths = ResolveModelPaths(language);
        return File.Exists(modelPaths.DetPath) &&
            File.Exists(modelPaths.ClsPath) &&
            File.Exists(modelPaths.RecPath) &&
            File.Exists(modelPaths.KeysPath);
    }

    private static string GetRecognizerModelFileName(OcrLanguageKind language)
    {
        return language switch
        {
            OcrLanguageKind.Japanese => "japan_PP-OCRv4_rec_mobile.onnx",
            OcrLanguageKind.Korean => "korean_PP-OCRv5_rec_mobile.onnx",
            _ => "latin_PP-OCRv5_rec_mobile_infer.onnx"
        };
    }

    private static string GetDictionaryFileName(OcrLanguageKind language)
    {
        return language switch
        {
            OcrLanguageKind.Japanese => "japan_dict.txt",
            OcrLanguageKind.Korean => "ppocrv5_korean_dict.txt",
            _ => "ppocrv5_latin_dict.txt"
        };
    }

    public Task<CoreOcrResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
    {
        if (!frame.HasImage)
        {
            return Task.FromResult(new CoreOcrResult(string.Empty));
        }

        return Task.Run(() => Recognize(frame, cancellationToken), cancellationToken);
    }

    public void Dispose()
    {
        _ocr.Dispose();
    }

    private CoreOcrResult Recognize(CapturedFrame frame, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var bitmap = CreateBitmap(frame);
        if (bitmap is null)
        {
            return new CoreOcrResult(string.Empty);
        }

        using var candidates = PixelOcrImagePreprocessor.CreateCandidates(bitmap);
        var results = new List<CoreOcrResult>(candidates.Items.Count);
        foreach (var candidate in candidates.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var detected = Detect(candidate.Bitmap, cancellationToken);
            results.Add(CreateResultFromImageGeometry(
                frame,
                candidate.Bitmap.Width,
                candidate.Bitmap.Height,
                ToTextLines(detected)));
        }

        return PixelOcrResultSelector.SelectBest(results);
    }

    private static SKBitmap? CreateBitmap(CapturedFrame frame)
    {
        if (!frame.HasRawBgra32Image)
        {
            return SKBitmap.Decode(frame.ImageBytes);
        }

        var bitmap = new SKBitmap(
            new SKImageInfo(
                frame.ImageWidthPixels,
                frame.ImageHeightPixels,
                SKColorType.Bgra8888,
                SKAlphaType.Premul));
        Marshal.Copy(frame.ImageBytes, 0, bitmap.GetPixels(), frame.ImageWidthPixels * frame.ImageHeightPixels * 4);
        return bitmap;
    }

    private RapidOcrNet.OcrResult Detect(SKBitmap bitmap, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _ocr.Detect(bitmap, _options);
        }
    }

    private static IReadOnlyList<OcrTextLine> ToTextLines(RapidOcrNet.OcrResult result)
    {
        return result.TextBlocks
            .Select(block => new OcrTextLine(
                block.Text,
                ToBounds(block.BoxPoints)))
            .Where(line => !string.IsNullOrWhiteSpace(line.Text))
            .ToArray();
    }

    internal static CoreOcrResult CreateResultFromImageGeometry(
        CapturedFrame frame,
        int imageWidthPixels,
        int imageHeightPixels,
        IReadOnlyList<OcrTextLine> imageLines)
    {
        var visualLines = MergeTextBlocksIntoVisualLines(imageLines);
        var geometry = OcrGeometryNormalizer.Normalize(
            frame.Region,
            imageWidthPixels,
            imageHeightPixels,
            EstimateTextBoundsPixels(visualLines),
            EstimateTextHeightPixels(visualLines),
            visualLines);

        return new CoreOcrResult(
            string.Join("\n", visualLines.Select(line => line.Text)),
            geometry.EstimatedTextHeightPixels,
            geometry.TextBoundsPixels,
            geometry.TextLines);
    }

    private static IReadOnlyList<OcrTextLine> MergeTextBlocksIntoVisualLines(IReadOnlyList<OcrTextLine> blocks)
    {
        var boundedBlocks = blocks
            .Where(block => !string.IsNullOrWhiteSpace(block.Text) && block.BoundsPixels.HasValue)
            .OrderBy(block => CenterY(block.BoundsPixels!.Value))
            .ThenBy(block => block.BoundsPixels!.Value.X)
            .ToArray();

        if (boundedBlocks.Length == 0)
        {
            return blocks;
        }

        var groups = new List<List<OcrTextLine>>();
        foreach (var block in boundedBlocks)
        {
            var bounds = block.BoundsPixels!.Value;
            var group = groups.FirstOrDefault(candidate => BelongsToVisualLine(candidate, bounds));
            if (group is null)
            {
                groups.Add([block]);
            }
            else
            {
                group.Add(block);
            }
        }

        return groups
            .Select(MergeVisualLine)
            .OrderBy(line => line.BoundsPixels?.Y ?? 0)
            .ThenBy(line => line.BoundsPixels?.X ?? 0)
            .ToArray();
    }

    private static bool BelongsToVisualLine(IReadOnlyList<OcrTextLine> group, FrameRegion bounds)
    {
        var groupBounds = MergeBounds(group.Select(line => line.BoundsPixels!.Value));
        var centerDelta = Math.Abs(CenterY(groupBounds) - CenterY(bounds));
        var tolerance = Math.Max(4, Math.Min(groupBounds.Height, bounds.Height) * 0.75);
        return centerDelta <= tolerance || VerticalOverlapRatio(groupBounds, bounds) >= 0.45;
    }

    private static OcrTextLine MergeVisualLine(IReadOnlyList<OcrTextLine> group)
    {
        var ordered = group
            .OrderBy(line => line.BoundsPixels!.Value.X)
            .ToArray();
        var text = string.Join(" ", ordered.Select(line => line.Text.Trim()));

        return new OcrTextLine(
            text,
            MergeBounds(ordered.Select(line => line.BoundsPixels!.Value)));
    }

    private static FrameRegion MergeBounds(IEnumerable<FrameRegion> bounds)
    {
        var regions = bounds.ToArray();
        var left = regions.Min(region => region.X);
        var top = regions.Min(region => region.Y);
        var right = regions.Max(region => region.X + region.Width);
        var bottom = regions.Max(region => region.Y + region.Height);

        return new FrameRegion(
            left,
            top,
            Math.Max(1, right - left),
            Math.Max(1, bottom - top));
    }

    private static double CenterY(FrameRegion region)
    {
        return region.Y + region.Height / 2.0;
    }

    private static double VerticalOverlapRatio(FrameRegion first, FrameRegion second)
    {
        var overlap = Math.Min(first.Y + first.Height, second.Y + second.Height) - Math.Max(first.Y, second.Y);
        if (overlap <= 0)
        {
            return 0;
        }

        return overlap / (double)Math.Min(first.Height, second.Height);
    }

    private static FrameRegion? ToBounds(IReadOnlyList<SKPointI> points)
    {
        if (points.Count == 0)
        {
            return null;
        }

        var left = points.Min(point => point.X);
        var top = points.Min(point => point.Y);
        var right = points.Max(point => point.X);
        var bottom = points.Max(point => point.Y);

        return new FrameRegion(
            left,
            top,
            Math.Max(1, right - left),
            Math.Max(1, bottom - top));
    }

    private static double? EstimateTextHeightPixels(IReadOnlyList<OcrTextLine> lines)
    {
        var heights = lines
            .Select(line => line.BoundsPixels?.Height)
            .Where(height => height is > 0)
            .Select(height => (double)height!.Value)
            .Order()
            .ToArray();

        if (heights.Length == 0)
        {
            return null;
        }

        return heights[heights.Length / 2];
    }

    private static FrameRegion? EstimateTextBoundsPixels(IReadOnlyList<OcrTextLine> lines)
    {
        var bounds = lines
            .Select(line => line.BoundsPixels)
            .Where(bounds => bounds.HasValue)
            .Select(bounds => bounds!.Value)
            .ToArray();

        if (bounds.Length == 0)
        {
            return null;
        }

        var left = bounds.Min(region => region.X);
        var top = bounds.Min(region => region.Y);
        var right = bounds.Max(region => region.X + region.Width);
        var bottom = bounds.Max(region => region.Y + region.Height);

        return new FrameRegion(
            left,
            top,
            Math.Max(1, right - left),
            Math.Max(1, bottom - top));
    }

    internal sealed record ModelPaths(string DetPath, string ClsPath, string RecPath, string KeysPath);
}
