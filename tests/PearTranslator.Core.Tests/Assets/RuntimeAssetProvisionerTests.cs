using System.Net;
using PearTranslator.Core.Assets;

namespace PearTranslator.Core.Tests.Assets;

public sealed class RuntimeAssetProvisionerTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task EnsureAssetsAsyncDownloadsMissingAssetsToRuntimeDirectory()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler("model-data"));
        var manifest = new RuntimeAssetManifest(
        [
            new RuntimeAssetDescriptor("test model", "models/test.onnx", new Uri("https://example.test/model.onnx"), 4)
        ]);
        var provisioner = new RuntimeAssetProvisioner(httpClient, manifest, _tempDirectory);

        await provisioner.EnsureAssetsAsync(progress: null, CancellationToken.None);

        var downloaded = await File.ReadAllTextAsync(Path.Combine(_tempDirectory, "models", "test.onnx"));
        Assert.Equal("model-data", downloaded);
    }

    [Fact]
    public async Task EnsureAssetsAsyncSkipsExistingLargeEnoughAssets()
    {
        var assetPath = Path.Combine(_tempDirectory, "models", "test.onnx");
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
        await File.WriteAllTextAsync(assetPath, "existing-data");
        using var httpClient = new HttpClient(new StubHttpMessageHandler("new-data"));
        var manifest = new RuntimeAssetManifest(
        [
            new RuntimeAssetDescriptor("test model", "models/test.onnx", new Uri("https://example.test/model.onnx"), 4)
        ]);
        var provisioner = new RuntimeAssetProvisioner(httpClient, manifest, _tempDirectory);

        await provisioner.EnsureAssetsAsync(progress: null, CancellationToken.None);

        Assert.Equal("existing-data", await File.ReadAllTextAsync(assetPath));
    }

    [Fact]
    public async Task EnsureAssetsAsyncSkipsAssetsThatExistInApplicationDirectory()
    {
        var userDirectory = Path.Combine(_tempDirectory, "user");
        var applicationDirectory = Path.Combine(_tempDirectory, "app");
        var applicationAssetPath = Path.Combine(applicationDirectory, "models", "test.onnx");
        Directory.CreateDirectory(Path.GetDirectoryName(applicationAssetPath)!);
        await File.WriteAllTextAsync(applicationAssetPath, "bundled-data");
        var handler = new StubHttpMessageHandler("downloaded-data");
        using var httpClient = new HttpClient(handler);
        var manifest = new RuntimeAssetManifest(
        [
            new RuntimeAssetDescriptor("test model", "models/test.onnx", new Uri("https://example.test/model.onnx"), 4)
        ]);
        var provisioner = new RuntimeAssetProvisioner(httpClient, manifest, userDirectory, applicationDirectory);

        await provisioner.EnsureAssetsAsync(progress: null, CancellationToken.None);

        Assert.False(File.Exists(Path.Combine(userDirectory, "models", "test.onnx")));
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task EnsureAssetsAsyncDownloadsOnlyAssetsMissingFromBothUserAndApplicationDirectories()
    {
        var userDirectory = Path.Combine(_tempDirectory, "user");
        var applicationDirectory = Path.Combine(_tempDirectory, "app");
        var bundledAssetPath = Path.Combine(applicationDirectory, "models", "bundled.onnx");
        Directory.CreateDirectory(Path.GetDirectoryName(bundledAssetPath)!);
        await File.WriteAllTextAsync(bundledAssetPath, "bundled-data");
        var handler = new StubHttpMessageHandler("downloaded-data");
        using var httpClient = new HttpClient(handler);
        var missingUri = new Uri("https://example.test/missing.onnx");
        var manifest = new RuntimeAssetManifest(
        [
            new RuntimeAssetDescriptor("bundled model", "models/bundled.onnx", new Uri("https://example.test/bundled.onnx"), 4),
            new RuntimeAssetDescriptor("missing model", "models/missing.onnx", missingUri, 4)
        ]);
        var provisioner = new RuntimeAssetProvisioner(httpClient, manifest, userDirectory, applicationDirectory);

        await provisioner.EnsureAssetsAsync(progress: null, CancellationToken.None);

        Assert.False(File.Exists(Path.Combine(userDirectory, "models", "bundled.onnx")));
        Assert.Equal("downloaded-data", await File.ReadAllTextAsync(Path.Combine(userDirectory, "models", "missing.onnx")));
        Assert.Equal([missingUri], handler.RequestUris);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _content;
        private readonly List<Uri> _requestUris = [];
        private int _sendCount;

        public StubHttpMessageHandler(string content)
        {
            _content = content;
        }

        public int SendCount => _sendCount;

        public IReadOnlyList<Uri> RequestUris => _requestUris;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _sendCount++;
            _requestUris.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_content)
            });
        }
    }
}
