using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;
using PearTranslator.Ocr.Windows;
using RapidOcrNet;

namespace PearTranslator.Ocr.Windows.Tests;

public sealed class RapidOcrEngineTests
{
    [Fact]
    public void ResolveModelPathsPrefersDownloadedUserRuntimeAssets()
    {
        var userRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "user");
        var appRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "app");
        try
        {
            var userDetPath = Path.Combine(userRoot, "models", "v6", "PP-OCRv6_small_det.onnx");
            var appDetPath = Path.Combine(appRoot, "models", "v6", "PP-OCRv6_small_det.onnx");
            Directory.CreateDirectory(Path.GetDirectoryName(userDetPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(appDetPath)!);
            File.WriteAllText(userDetPath, "user");
            File.WriteAllText(appDetPath, "app");

            var modelPaths = RapidOcrEngine.ResolveModelPaths(OcrLanguageKind.English, userRoot, appRoot);

            Assert.Equal(userDetPath, modelPaths.DetPath);
        }
        finally
        {
            if (Directory.Exists(userRoot))
            {
                Directory.Delete(Path.GetDirectoryName(userRoot)!, recursive: true);
            }

            if (Directory.Exists(appRoot))
            {
                Directory.Delete(Path.GetDirectoryName(appRoot)!, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(OcrLanguageKind.Auto)]
    [InlineData(OcrLanguageKind.English)]
    [InlineData(OcrLanguageKind.Chinese)]
    [InlineData(OcrLanguageKind.Japanese)]
    public void ResolveModelPathsUsesPpOcrV6SmallForUnifiedModelLanguages(OcrLanguageKind language)
    {
        var modelPaths = RapidOcrEngine.ResolveModelPaths(language);

        Assert.EndsWith(Path.Combine("models", "v6", "PP-OCRv6_small_det.onnx"), modelPaths.DetPath);
        Assert.EndsWith(Path.Combine("models", "v5", "ch_ppocr_mobile_v2.0_cls_infer.onnx"), modelPaths.ClsPath);
        Assert.EndsWith(Path.Combine("models", "v6", "PP-OCRv6_small_rec.onnx"), modelPaths.RecPath);
        Assert.EndsWith(Path.Combine("models", "v6", "ppocrv6_dict.txt"), modelPaths.KeysPath);
    }

    [Theory]
    [InlineData(OcrLanguageKind.Auto)]
    [InlineData(OcrLanguageKind.English)]
    [InlineData(OcrLanguageKind.Chinese)]
    [InlineData(OcrLanguageKind.Japanese)]
    public void TryCreateInitializesPpOcrV6SmallForUnifiedModelLanguages(OcrLanguageKind language)
    {
        if (!RapidOcrEngine.TryCreate(language, out var engine))
        {
            Assert.Fail(DescribeInitializationFailure(language));
        }

        engine.Dispose();
    }

    [Fact]
    public void ResolveModelPathsUsesRapidOcrCompatiblePpOcrV6Dictionary()
    {
        var modelPaths = RapidOcrEngine.ResolveModelPaths(OcrLanguageKind.English);
        var firstLine = File.ReadLines(modelPaths.KeysPath).First();

        Assert.False(string.IsNullOrEmpty(firstLine));
        Assert.Equal("!", firstLine);
    }

    private static string DescribeInitializationFailure(OcrLanguageKind language)
    {
        var modelPaths = RapidOcrEngine.ResolveModelPaths(language);
        using var sessionOptions = RapidOcr.GetDefaultSessionOptions();
        var ocr = new RapidOcr();
        var exception = Record.Exception(() =>
        {
            ocr.InitModels(
                modelPaths.DetPath,
                modelPaths.ClsPath,
                modelPaths.RecPath,
                modelPaths.KeysPath,
                sessionOptions);
        });

        try
        {
            ocr.Dispose();
        }
        catch
        {
            // Some failed RapidOCR initializations leave nested native wrappers null.
        }

        return exception?.ToString() ?? "RapidOCR initialization returned false without an exception.";
    }

    [Fact]
    public void ResolveModelPathsKeepsExistingMobileRecognizerForKorean()
    {
        var modelPaths = RapidOcrEngine.ResolveModelPaths(OcrLanguageKind.Korean);

        Assert.EndsWith(Path.Combine("models", "v5", "ch_PP-OCRv5_mobile_det.onnx"), modelPaths.DetPath);
        Assert.EndsWith(Path.Combine("models", "v5", "korean_PP-OCRv5_rec_mobile.onnx"), modelPaths.RecPath);
        Assert.EndsWith(Path.Combine("models", "v5", "ppocrv5_korean_dict.txt"), modelPaths.KeysPath);
    }

    [Fact]
    public void CreateResultScalesImageLineBoundsToCaptureRegion()
    {
        var frame = new CapturedFrame(
            new FrameRegion(100, 200, 800, 240),
            DateTimeOffset.UtcNow,
            [1],
            [2],
            "image/png");
        var imageLines = new[]
        {
            new OcrTextLine("Open the door", new FrameRegion(125, 75, 500, 30)),
            new OcrTextLine("Take the key", new FrameRegion(125, 125, 450, 30))
        };

        var result = RapidOcrEngine.CreateResultFromImageGeometry(
            frame,
            imageWidthPixels: 1000,
            imageHeightPixels: 300,
            imageLines);

        Assert.Equal("Open the door\nTake the key", result.Text);
        Assert.Equal(24, result.EstimatedTextHeightPixels);
        Assert.NotNull(result.TextLines);
        Assert.Equal(new FrameRegion(100, 60, 400, 24), result.TextLines[0].BoundsPixels);
        Assert.Equal(new FrameRegion(100, 100, 360, 24), result.TextLines[1].BoundsPixels);
        Assert.Equal(new FrameRegion(100, 60, 400, 64), result.TextBoundsPixels);
    }

    [Fact]
    public void CreateResultMergesTextBlocksOnTheSameVisualRow()
    {
        var frame = new CapturedFrame(
            new FrameRegion(0, 0, 800, 220),
            DateTimeOffset.UtcNow,
            [1],
            [2],
            "image/png");
        var imageBlocks = new[]
        {
            new OcrTextLine("Maybe", new FrameRegion(100, 40, 80, 24)),
            new OcrTextLine("we can", new FrameRegion(190, 39, 110, 26)),
            new OcrTextLine("call room service", new FrameRegion(310, 42, 260, 22)),
            new OcrTextLine("and have breakfast in bed.", new FrameRegion(100, 86, 420, 24))
        };

        var result = RapidOcrEngine.CreateResultFromImageGeometry(
            frame,
            imageWidthPixels: 800,
            imageHeightPixels: 220,
            imageBlocks);

        Assert.Equal("Maybe we can call room service\nand have breakfast in bed.", result.Text);
        Assert.NotNull(result.TextLines);
        Assert.Collection(
            result.TextLines,
            line =>
            {
                Assert.Equal("Maybe we can call room service", line.Text);
                Assert.Equal(new FrameRegion(100, 39, 470, 26), line.BoundsPixels);
            },
            line =>
            {
                Assert.Equal("and have breakfast in bed.", line.Text);
                Assert.Equal(new FrameRegion(100, 86, 420, 24), line.BoundsPixels);
            });
    }
}
