namespace PearTranslator.Core.Assets;

public sealed class RuntimeAssetManifest
{
    public static RuntimeAssetManifest Default { get; } = new(
    [
        new RuntimeAssetDescriptor(
            "ECDICT 字典",
            "Resources/ecdict.csv",
            new Uri("https://raw.githubusercontent.com/skywind3000/ECDICT/master/ecdict.csv"),
            1_000_000),
        new RuntimeAssetDescriptor(
            "ECDICT 许可证",
            "Resources/ecdict-LICENSE.txt",
            new Uri("https://raw.githubusercontent.com/skywind3000/ECDICT/master/LICENSE"),
            500),
        new RuntimeAssetDescriptor(
            "PaddleOCR 许可证",
            "Licenses/PaddleOCR-LICENSE.txt",
            new Uri("https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/LICENSE"),
            500),
        new RuntimeAssetDescriptor(
            "PP-OCRv6 检测模型",
            "models/v6/PP-OCRv6_small_det.onnx",
            new Uri("https://huggingface.co/PaddlePaddle/PP-OCRv6_small_det_onnx/resolve/main/inference.onnx"),
            5_000_000),
        new RuntimeAssetDescriptor(
            "PP-OCRv6 识别模型",
            "models/v6/PP-OCRv6_small_rec.onnx",
            new Uri("https://huggingface.co/PaddlePaddle/PP-OCRv6_small_rec_onnx/resolve/main/inference.onnx"),
            10_000_000),
        new RuntimeAssetDescriptor(
            "PP-OCRv6 字典",
            "models/v6/ppocrv6_dict.txt",
            new Uri("https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/ppocr/utils/dict/ppocrv6_dict.txt"),
            50_000),
        new RuntimeAssetDescriptor(
            "韩文 PP-OCRv5 识别模型",
            "models/v5/korean_PP-OCRv5_rec_mobile.onnx",
            new Uri("https://huggingface.co/PaddlePaddle/korean_PP-OCRv5_mobile_rec_onnx/resolve/main/inference.onnx"),
            5_000_000),
        new RuntimeAssetDescriptor(
            "韩文 PP-OCRv5 字典",
            "models/v5/ppocrv5_korean_dict.txt",
            new Uri("https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/ppocr/utils/dict/ppocrv5_korean_dict.txt"),
            10_000)
    ]);

    public RuntimeAssetManifest(IReadOnlyList<RuntimeAssetDescriptor> assets)
    {
        Assets = assets;
    }

    public IReadOnlyList<RuntimeAssetDescriptor> Assets { get; }

    public RuntimeAssetStatus GetStatus(string rootDirectory)
    {
        var missing = Assets
            .Where(asset => !IsConfigured(rootDirectory, asset))
            .ToArray();

        return new RuntimeAssetStatus(missing.Length == 0, missing);
    }

    public RuntimeAssetStatus GetStatus(string userAssetRootDirectory, string applicationRootDirectory)
    {
        var rootDirectories = new[] { userAssetRootDirectory, applicationRootDirectory };
        var missing = Assets
            .Where(asset => !IsConfigured(rootDirectories, asset))
            .ToArray();

        return new RuntimeAssetStatus(missing.Length == 0, missing);
    }

    private static bool IsConfigured(IEnumerable<string> rootDirectories, RuntimeAssetDescriptor asset)
    {
        return rootDirectories
            .Where(rootDirectory => !string.IsNullOrWhiteSpace(rootDirectory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Any(rootDirectory => IsConfigured(rootDirectory, asset));
    }

    private static bool IsConfigured(string rootDirectory, RuntimeAssetDescriptor asset)
    {
        var path = RuntimeAssetLocator.Combine(rootDirectory, asset.RelativePath);
        if (!File.Exists(path))
        {
            return false;
        }

        return new FileInfo(path).Length >= asset.MinimumBytes;
    }
}
