using System.Net.Http;
using System.IO;
using System.Windows.Threading;
using PearTranslator.App.Wpf.Mocks;
using PearTranslator.App.Wpf.Overlay;
using PearTranslator.App.Wpf.Providers;
using PearTranslator.Capture.Windows;
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Assets;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Control;
using PearTranslator.Core.Pipeline;
using PearTranslator.Core.Translation;
using PearTranslator.Ocr.Windows;
using PearTranslator.Translate.OpenAI;
using PearTranslator.Translate.Traditional;

namespace PearTranslator.App.Wpf;

public sealed class CompositionRoot
{
    private readonly DispatcherTimer _timer;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly HttpClient _systemProxyHttpClient = CreateHttpClient(useSystemProxy: true);
    private readonly HttpClient _directHttpClient = CreateHttpClient(useSystemProxy: false);
    private readonly IOverlayPresenter _overlayPresenter;
    private readonly TranslatorOptions _baseOptions = new();
    private FirstWordLocalPreviewProvider _localPreviewProvider;
    private TranslatorSettings _settings;
    private bool _isTicking;
    private int _connectionProbeVersion;

    public CompositionRoot(IOverlayPresenter overlayPresenter, TranslatorSettings settings)
    {
        _overlayPresenter = overlayPresenter;
        _settings = settings;
        Controller = new TranslatorController();
        Capture = new WindowsRegionCapture();
        _localPreviewProvider = CreateLocalPreviewProvider();
        Loop = CreateLoop(settings);
        _ = WarmUpLocalPreviewProviderAsync();

        _timer = new DispatcherTimer { Interval = _baseOptions.SamplingInterval };
        _timer.Tick += async (_, _) => await TickAsync();
    }

    public TranslatorController Controller { get; }

    public WindowsRegionCapture Capture { get; }

    public SubtitleTranslationLoop Loop { get; private set; }

    public event EventHandler<string>? StatusChanged;

    public event EventHandler<string>? ConnectionStatusChanged;

    public RuntimeAssetStatus GetRuntimeAssetStatus()
    {
        return RuntimeAssetManifest.Default.GetStatus(
            RuntimeAssetLocator.DefaultUserAssetRootDirectory,
            AppContext.BaseDirectory);
    }

    public async Task ConfigureRuntimeAssetsAsync(
        IProgress<RuntimeAssetSetupProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _shutdown.Token,
            cancellationToken);
        var provisioner = new RuntimeAssetProvisioner(
            _systemProxyHttpClient,
            RuntimeAssetManifest.Default,
            RuntimeAssetLocator.DefaultUserAssetRootDirectory,
            AppContext.BaseDirectory);

        await provisioner.EnsureAssetsAsync(progress, linkedCancellation.Token);
        ReloadRuntimeAssets();
    }

    public void Start()
    {
        _timer.Start();
        _ = TickAsync();
    }

    public void Stop()
    {
        _timer.Stop();
        _shutdown.Cancel();
        Loop.Dispose();
        _systemProxyHttpClient.Dispose();
        _directHttpClient.Dispose();
    }

    public void ApplySettings(TranslatorSettings settings)
    {
        _settings = settings;
        Interlocked.Increment(ref _connectionProbeVersion);
        var oldLoop = Loop;
        Loop = CreateLoop(settings);
        DisposeLoopWhenIdle(oldLoop);
    }

    public async Task TestTranslationConnectionAsync(CancellationToken cancellationToken)
    {
        var version = Interlocked.Increment(ref _connectionProbeVersion);
        var settings = _settings;
        if (!TryCreatePrimaryTranslationProvider(settings, out var provider))
        {
            return;
        }

        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _shutdown.Token,
            cancellationToken);
        var token = linkedCancellation.Token;

        ConnectionStatusChanged?.Invoke(
            this,
            TranslationConnectionStatusFormatter.FormatPreflightStarting(settings));

        TranslationConnectionProbeResult result;
        try
        {
            result = await TranslationConnectionProbe.ProbeAsync(provider, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return;
        }

        if (version != _connectionProbeVersion || token.IsCancellationRequested)
        {
            return;
        }

        ConnectionStatusChanged?.Invoke(
            this,
            result.Succeeded
                ? TranslationConnectionStatusFormatter.Format(
                    settings,
                    new TranslationTelemetry(result.ProviderLabel, result.Latency))
                : TranslationConnectionStatusFormatter.FormatPreflightFailure(settings, result));
    }

    private async Task TickAsync()
    {
        if (_isTicking)
        {
            return;
        }

        _isTicking = true;
        try
        {
            var result = await Loop.TryTickAsync(_shutdown.Token);
            PublishConnectionStatus(result);
            if (result.Outcome == TranslationLoopOutcome.Failed)
            {
                StatusChanged?.Invoke(
                    this,
                    string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "翻译出错。请检查翻译服务设置、网络或 API 密钥。"
                        : $"翻译出错：{result.ErrorMessage}");
            }
        }
        finally
        {
            _isTicking = false;
        }
    }

    private void DisposeLoopWhenIdle(SubtitleTranslationLoop loop)
    {
        if (!_isTicking)
        {
            loop.Dispose();
            return;
        }

        _ = DisposeLoopWhenIdleAsync(loop);
    }

    private async Task DisposeLoopWhenIdleAsync(SubtitleTranslationLoop loop)
    {
        try
        {
            while (_isTicking && !_shutdown.IsCancellationRequested)
            {
                await Task.Delay(50, _shutdown.Token);
            }

            if (!ReferenceEquals(loop, Loop))
            {
                loop.Dispose();
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
    }

    public Task<TranslationLoopResult> RunOneShotAsync(CancellationToken cancellationToken)
    {
        return RunAndPublishOneShotAsync(cancellationToken);
    }

    public async Task ToggleDismissCurrentAsync(CancellationToken cancellationToken)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _shutdown.Token,
            cancellationToken);
        var token = linkedCancellation.Token;

        if (Controller.State == TranslatorRunState.Dismissed)
        {
            Controller.RestoreDismissed();
            await Loop.TryRestoreRealtimeOverlayAsync(token);
            return;
        }

        if (Controller.State == TranslatorRunState.Running)
        {
            Controller.DismissCurrent();
            await Loop.HideRealtimeOverlayAsync(token);
        }
    }

    public async Task<TranslationLoopResult> RunOneShotAsync(
        FrameRegion region,
        CancellationToken cancellationToken)
    {
        var frame = await Capture.CaptureRegionAsync(region, cancellationToken);
        var result = await Loop.TryRunOneShotAsync(frame, cancellationToken);
        PublishConnectionStatus(result);
        return result;
    }

    private async Task<TranslationLoopResult> RunAndPublishOneShotAsync(CancellationToken cancellationToken)
    {
        var result = await Loop.TryRunOneShotAsync(cancellationToken);
        PublishConnectionStatus(result);
        return result;
    }

    public async Task<IReadOnlyList<string>> ListOpenAiCompatibleModelsAsync(
        OpenAiProviderSettings settings,
        CancellationToken cancellationToken)
    {
        if (!OpenAiTranslationOptions.TryCreate(
            settings,
            Environment.GetEnvironmentVariable,
            out var options) ||
            options is null)
        {
            throw new InvalidOperationException("请先填写 API 密钥，或设置 OPENAI_API_KEY。");
        }

        var httpClient = SelectOpenAiHttpClient(options);
        var client = new OpenAiCompatibleModelCatalogClient(httpClient);
        return await client.ListModelsAsync(options, cancellationToken);
    }

    private SubtitleTranslationLoop CreateLoop(TranslatorSettings settings)
    {
        return new SubtitleTranslationLoop(
            Controller,
            Capture,
            new FingerprintFrameChangeDetector(),
            CreateOcrEngine(settings.Ocr),
            CreateTranslationProvider(SelectTranslationHttpClient(settings), settings),
            _overlayPresenter,
            new TranslationCache(),
            CreateOptions(settings),
            _localPreviewProvider);
    }

    private static FirstWordLocalPreviewProvider CreateLocalPreviewProvider()
    {
        var dictionaryPath = RuntimeAssetLocator.ResolvePath("Resources/ecdict.csv");
        return new FirstWordLocalPreviewProvider(new EcdictTranslationProvider(dictionaryPath));
    }

    private void ReloadRuntimeAssets()
    {
        _localPreviewProvider = CreateLocalPreviewProvider();
        _ = WarmUpLocalPreviewProviderAsync();
        var oldLoop = Loop;
        Loop = CreateLoop(_settings);
        DisposeLoopWhenIdle(oldLoop);
    }

    private async Task WarmUpLocalPreviewProviderAsync()
    {
        try
        {
            await _localPreviewProvider.WarmUpAsync(_shutdown.Token);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch
        {
        }
    }

    private TranslatorOptions CreateOptions(TranslatorSettings settings)
    {
        return new TranslatorOptions
        {
            RequiredStableRepeats = _baseOptions.RequiredStableRepeats,
            SamplingInterval = _baseOptions.SamplingInterval,
            ShowOcrPositionOnly = settings.Overlay.OcrPositionTest,
            AlignTranslationToOcrLines = settings.Overlay.PositionOverlay,
            LocalPreviewEnabled = settings.Overlay.LocalPreviewEnabled,
            TargetLanguage = settings.Translation.TargetLanguage
        };
    }

    private static IOcrEngine CreateOcrEngine(OcrSettings settings)
    {
        return OcrEngineFactory.TryCreate(settings) ?? new MockOcrEngine();
    }

    private HttpClient SelectTranslationHttpClient(TranslatorSettings settings)
    {
        if (settings.Translation.Provider == TranslationProviderKind.OpenAi &&
            OpenAiTranslationOptions.TryCreate(
                settings.Translation.OpenAi,
                Environment.GetEnvironmentVariable,
                settings.Translation.TargetLanguage,
                out var options) &&
            options is not null)
        {
            return SelectOpenAiHttpClient(options);
        }

        return _systemProxyHttpClient;
    }

    private HttpClient SelectOpenAiHttpClient(OpenAiTranslationOptions options)
    {
        return options.UseSystemProxy ? _systemProxyHttpClient : _directHttpClient;
    }

    private void PublishConnectionStatus(TranslationLoopResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ConnectionStatusMessage))
        {
            ConnectionStatusChanged?.Invoke(
                this,
                TranslationConnectionStatusFormatter.FormatNoRequest(
                    _settings,
                    result.ConnectionStatusMessage));
            return;
        }

        if (result.Telemetry is not { } telemetry)
        {
            return;
        }

        ConnectionStatusChanged?.Invoke(
            this,
            TranslationConnectionStatusFormatter.Format(_settings, telemetry));
    }

    private static ITranslationProvider CreateTranslationProvider(HttpClient httpClient, TranslatorSettings settings)
    {
        return settings.Translation.Provider switch
        {
            TranslationProviderKind.OpenAi
                when OpenAiTranslationOptions.TryCreate(
                    settings.Translation.OpenAi,
                    Environment.GetEnvironmentVariable,
                    settings.Translation.TargetLanguage,
                    out var options) && options is not null
                => CreateFallbackProvider(new OpenAiTranslationProvider(httpClient, options)),
            TranslationProviderKind.Mock => new MockTranslationProvider(),
            TranslationProviderKind.Azure when TryCreateAzure(
                settings.Translation.Azure,
                settings.Translation.TargetLanguage,
                out var azureOptions)
                => CreateFallbackProvider(new AzureTranslatorProvider(httpClient, azureOptions)),
            TranslationProviderKind.DeepL when TryCreateDeepL(
                settings.Translation.DeepL,
                settings.Translation.TargetLanguage,
                out var deepLOptions)
                => CreateFallbackProvider(new DeepLTranslatorProvider(httpClient, deepLOptions)),
            TranslationProviderKind.Google when TryCreateGoogle(
                settings.Translation.Google,
                settings.Translation.TargetLanguage,
                out var googleOptions)
                => CreateFallbackProvider(new GoogleTranslateProvider(httpClient, googleOptions)),
            _ => new OcrPreviewTranslationProvider()
        };
    }

    private bool TryCreatePrimaryTranslationProvider(
        TranslatorSettings settings,
        out ITranslationProvider provider)
    {
        provider = null!;
        switch (settings.Translation.Provider)
        {
            case TranslationProviderKind.OpenAi
                when OpenAiTranslationOptions.TryCreate(
                    settings.Translation.OpenAi,
                    Environment.GetEnvironmentVariable,
                    settings.Translation.TargetLanguage,
                    out var options) &&
                options is not null:
                provider = new OpenAiTranslationProvider(SelectOpenAiHttpClient(options), options);
                return true;
            case TranslationProviderKind.Azure when TryCreateAzure(
                settings.Translation.Azure,
                settings.Translation.TargetLanguage,
                out var azureOptions):
                provider = new AzureTranslatorProvider(_systemProxyHttpClient, azureOptions);
                return true;
            case TranslationProviderKind.DeepL when TryCreateDeepL(
                settings.Translation.DeepL,
                settings.Translation.TargetLanguage,
                out var deepLOptions):
                provider = new DeepLTranslatorProvider(_systemProxyHttpClient, deepLOptions);
                return true;
            case TranslationProviderKind.Google when TryCreateGoogle(
                settings.Translation.Google,
                settings.Translation.TargetLanguage,
                out var googleOptions):
                provider = new GoogleTranslateProvider(_systemProxyHttpClient, googleOptions);
                return true;
            default:
                return false;
        }
    }

    private static ITranslationProvider CreateFallbackProvider(ITranslationProvider primaryProvider)
    {
        return new FallbackTranslationProvider(
            [primaryProvider, new OcrPreviewTranslationProvider()]);
    }

    private static bool TryCreateAzure(
        TraditionalProviderSettings settings,
        TargetLanguage targetLanguage,
        out TraditionalTranslationOptions options)
    {
        options = null!;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return false;
        }

        options = new TraditionalTranslationOptions(
            settings.ApiKey,
            ReadEndpoint(settings.Endpoint, "https://api.cognitive.microsofttranslator.com/"),
            Region: settings.Region,
            TargetLanguage: targetLanguage);
        return true;
    }

    private static bool TryCreateDeepL(
        TraditionalProviderSettings settings,
        TargetLanguage targetLanguage,
        out TraditionalTranslationOptions options)
    {
        options = null!;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return false;
        }

        options = new TraditionalTranslationOptions(
            settings.ApiKey,
            ReadEndpoint(settings.Endpoint, "https://api-free.deepl.com/v2/"),
            TargetLanguage: targetLanguage);
        return true;
    }

    private static bool TryCreateGoogle(
        TraditionalProviderSettings settings,
        TargetLanguage targetLanguage,
        out TraditionalTranslationOptions options)
    {
        options = null!;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return false;
        }

        options = new TraditionalTranslationOptions(
            settings.ApiKey,
            ReadEndpoint(settings.Endpoint, "https://translation.googleapis.com/language/translate/v2"),
            Project: settings.Project,
            TargetLanguage: targetLanguage);
        return true;
    }

    private static Uri ReadEndpoint(string value, string fallback)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var endpoint)
            ? endpoint
            : new Uri(fallback);
    }

    private static HttpClient CreateHttpClient(bool useSystemProxy)
    {
        return new HttpClient(new SocketsHttpHandler { UseProxy = useSystemProxy })
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
    }
}
