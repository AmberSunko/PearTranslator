namespace PearTranslator.Core.Assets;

public sealed class RuntimeAssetProvisioner
{
    private readonly HttpClient _httpClient;
    private readonly RuntimeAssetManifest _manifest;
    private readonly string _rootDirectory;
    private readonly string? _applicationRootDirectory;

    public RuntimeAssetProvisioner(
        HttpClient httpClient,
        RuntimeAssetManifest manifest,
        string rootDirectory)
        : this(httpClient, manifest, rootDirectory, applicationRootDirectory: null)
    {
    }

    public RuntimeAssetProvisioner(
        HttpClient httpClient,
        RuntimeAssetManifest manifest,
        string rootDirectory,
        string? applicationRootDirectory)
    {
        _httpClient = httpClient;
        _manifest = manifest;
        _rootDirectory = rootDirectory;
        _applicationRootDirectory = applicationRootDirectory;
    }

    public async Task EnsureAssetsAsync(
        IProgress<RuntimeAssetSetupProgress>? progress,
        CancellationToken cancellationToken)
    {
        var missingAssets = GetStatus().MissingAssets;
        var completed = _manifest.Assets.Count - missingAssets.Count;
        progress?.Report(new RuntimeAssetSetupProgress("检查资源", completed, _manifest.Assets.Count));

        foreach (var asset in missingAssets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new RuntimeAssetSetupProgress($"下载 {asset.Name}", completed, _manifest.Assets.Count));
            await DownloadAssetAsync(asset, cancellationToken);
            completed++;
            progress?.Report(new RuntimeAssetSetupProgress($"已配置 {asset.Name}", completed, _manifest.Assets.Count));
        }

        progress?.Report(new RuntimeAssetSetupProgress("模型和字典已配置", _manifest.Assets.Count, _manifest.Assets.Count));
    }

    private RuntimeAssetStatus GetStatus()
    {
        return string.IsNullOrWhiteSpace(_applicationRootDirectory)
            ? _manifest.GetStatus(_rootDirectory)
            : _manifest.GetStatus(_rootDirectory, _applicationRootDirectory);
    }

    private async Task DownloadAssetAsync(RuntimeAssetDescriptor asset, CancellationToken cancellationToken)
    {
        var destination = RuntimeAssetLocator.Combine(_rootDirectory, asset.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + ".download";
        if (File.Exists(temporary))
        {
            File.Delete(temporary);
        }

        using (var response = await _httpClient.GetAsync(asset.Uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = File.Create(temporary);
            await source.CopyToAsync(target, cancellationToken);
        }

        var downloaded = new FileInfo(temporary);
        if (downloaded.Length < asset.MinimumBytes)
        {
            File.Delete(temporary);
            throw new InvalidOperationException($"下载的资源文件过小：{asset.Name}");
        }

        File.Move(temporary, destination, overwrite: true);
    }
}
