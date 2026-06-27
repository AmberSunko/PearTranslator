using PearTranslator.Core.Assets;

namespace PearTranslator.Core.Tests.Assets;

public sealed class RuntimeAssetLocatorTests : IDisposable
{
    private readonly string _userAssetDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "user");
    private readonly string _applicationDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "app");

    [Fact]
    public void ResolvePathPrefersUserDownloadedAsset()
    {
        var userAssetPath = Path.Combine(_userAssetDirectory, "Resources", "ecdict.csv");
        var applicationAssetPath = Path.Combine(_applicationDirectory, "Resources", "ecdict.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(userAssetPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(applicationAssetPath)!);
        File.WriteAllText(userAssetPath, "user");
        File.WriteAllText(applicationAssetPath, "app");

        var resolved = RuntimeAssetLocator.ResolvePath(
            "Resources/ecdict.csv",
            _userAssetDirectory,
            _applicationDirectory);

        Assert.Equal(userAssetPath, resolved);
    }

    [Fact]
    public void ResolvePathFallsBackToApplicationAsset()
    {
        var applicationAssetPath = Path.Combine(_applicationDirectory, "Resources", "ecdict.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(applicationAssetPath)!);
        File.WriteAllText(applicationAssetPath, "app");

        var resolved = RuntimeAssetLocator.ResolvePath(
            "Resources/ecdict.csv",
            _userAssetDirectory,
            _applicationDirectory);

        Assert.Equal(applicationAssetPath, resolved);
    }

    public void Dispose()
    {
        var root = Path.GetDirectoryName(_userAssetDirectory);
        if (root is not null && Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        root = Path.GetDirectoryName(_applicationDirectory);
        if (root is not null && Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
