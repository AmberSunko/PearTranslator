using PearTranslator.Core.Assets;

namespace PearTranslator.Core.Tests.Assets;

public sealed class RuntimeAssetManifestTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public void DefaultManifestContainsRuntimeOcrModelsAndEcdictDictionary()
    {
        var relativePaths = RuntimeAssetManifest.Default.Assets
            .Select(asset => asset.RelativePath)
            .ToArray();

        Assert.Contains("Resources/ecdict.csv", relativePaths);
        Assert.Contains("Resources/ecdict-LICENSE.txt", relativePaths);
        Assert.Contains("Licenses/PaddleOCR-LICENSE.txt", relativePaths);
        Assert.Contains("models/v6/PP-OCRv6_small_det.onnx", relativePaths);
        Assert.Contains("models/v6/PP-OCRv6_small_rec.onnx", relativePaths);
        Assert.Contains("models/v6/ppocrv6_dict.txt", relativePaths);
        Assert.Contains("models/v5/korean_PP-OCRv5_rec_mobile.onnx", relativePaths);
        Assert.Contains("models/v5/ppocrv5_korean_dict.txt", relativePaths);
    }

    [Fact]
    public void GetStatusReportsMissingAssetsWhenFilesDoNotExist()
    {
        var manifest = new RuntimeAssetManifest(
        [
            new RuntimeAssetDescriptor("test model", "models/test.onnx", new Uri("https://example.test/model.onnx"), 4)
        ]);

        var status = manifest.GetStatus(_tempDirectory);

        Assert.False(status.IsComplete);
        Assert.Equal("models/test.onnx", Assert.Single(status.MissingAssets).RelativePath);
    }

    [Fact]
    public void GetStatusTreatsExistingLargeEnoughFilesAsConfigured()
    {
        var assetPath = Path.Combine(_tempDirectory, "models", "test.onnx");
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
        File.WriteAllBytes(assetPath, [1, 2, 3, 4]);
        var manifest = new RuntimeAssetManifest(
        [
            new RuntimeAssetDescriptor("test model", "models/test.onnx", new Uri("https://example.test/model.onnx"), 4)
        ]);

        var status = manifest.GetStatus(_tempDirectory);

        Assert.True(status.IsComplete);
        Assert.Empty(status.MissingAssets);
    }

    [Fact]
    public void GetStatusTreatsApplicationAssetsAsConfiguredWhenUserAssetsAreMissing()
    {
        var userDirectory = Path.Combine(_tempDirectory, "user");
        var applicationDirectory = Path.Combine(_tempDirectory, "app");
        var assetPath = Path.Combine(applicationDirectory, "models", "test.onnx");
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
        File.WriteAllBytes(assetPath, [1, 2, 3, 4]);
        var manifest = new RuntimeAssetManifest(
        [
            new RuntimeAssetDescriptor("test model", "models/test.onnx", new Uri("https://example.test/model.onnx"), 4)
        ]);

        var status = manifest.GetStatus(userDirectory, applicationDirectory);

        Assert.True(status.IsComplete);
        Assert.Empty(status.MissingAssets);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
