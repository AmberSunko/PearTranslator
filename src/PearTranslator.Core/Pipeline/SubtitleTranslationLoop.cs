using System.Diagnostics;
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Control;
using PearTranslator.Core.Text;
using PearTranslator.Core.Translation;

namespace PearTranslator.Core.Pipeline;

public sealed class SubtitleTranslationLoop : IDisposable
{
    private readonly TranslatorController _controller;
    private readonly IRegionCapture _capture;
    private readonly IFrameChangeDetector _frameChangeDetector;
    private readonly IOcrEngine _ocrEngine;
    private readonly ITranslationProvider _translationProvider;
    private readonly IOverlayPresenter _overlayPresenter;
    private readonly TranslationCache _translationCache;
    private readonly TextStabilizer _textStabilizer;
    private readonly FirstWordLocalPreviewProvider? _localPreviewProvider;
    private readonly bool _showOcrPositionOnly;
    private readonly bool _alignTranslationToOcrLines;
    private readonly bool _localPreviewEnabled;
    private readonly TargetLanguage _targetLanguage;
    private OcrResult? _lastRecognized;
    private FrameRegion? _lastRecognizedRegion;
    private string? _lastLocalPreviewSourceText;
    private string? _lastRealtimeOverlayText;
    private string? _lastRealtimeProviderLabel;
    private RealtimeOverlaySnapshot? _lastRealtimeOverlaySnapshot;
    private string? _stabilizationCandidateText;
    private long _stabilizationCandidateFirstSeenTimestamp;
    private int _translationRequestCount;
    private bool _hasVisibleRealtimeOverlay;
    private bool _disposed;

    public SubtitleTranslationLoop(
        TranslatorController controller,
        IRegionCapture capture,
        IFrameChangeDetector frameChangeDetector,
        IOcrEngine ocrEngine,
        ITranslationProvider translationProvider,
        IOverlayPresenter overlayPresenter,
        TranslationCache translationCache,
        TranslatorOptions options,
        FirstWordLocalPreviewProvider? localPreviewProvider = null)
    {
        _controller = controller;
        _capture = capture;
        _frameChangeDetector = frameChangeDetector;
        _ocrEngine = ocrEngine;
        _translationProvider = translationProvider;
        _overlayPresenter = overlayPresenter;
        _translationCache = translationCache;
        _textStabilizer = new TextStabilizer(options.RequiredStableRepeats);
        _localPreviewProvider = localPreviewProvider;
        _showOcrPositionOnly = options.ShowOcrPositionOnly;
        _alignTranslationToOcrLines = options.AlignTranslationToOcrLines;
        _localPreviewEnabled = options.LocalPreviewEnabled;
        _targetLanguage = options.TargetLanguage;
    }

    public async Task TickAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_controller.ShouldCapture)
        {
            await HideRealtimeOverlayAsync(cancellationToken);
            return;
        }

        var frame = await _capture.CaptureAsync(cancellationToken);
        var recognized = await RecognizeChangedOrCachedAsync(frame, cancellationToken);
        if (recognized is null)
        {
            return;
        }

        if (IsStaleRealtimeFrame(frame))
        {
            return;
        }

        if (!_showOcrPositionOnly && IsLikelyVisibleRealtimeOverlayEcho(recognized.Text))
        {
            return;
        }

        if (!_showOcrPositionOnly && _controller.ShouldShowOverlay)
        {
            await ShowLocalPreviewAsync(recognized, frame.Region, recognized.Text, isOneShot: false, cancellationToken);
        }

        var stableText = _textStabilizer.Observe(recognized.Text);
        if (stableText is null)
        {
            return;
        }

        _controller.AcceptStableSourceText(stableText);
        var translatableText = SourceTextTranslationExtractor.ExtractTranslatableText(stableText, _targetLanguage);
        if (!SourceTextTranslationFilter.ShouldTranslate(translatableText, _targetLanguage))
        {
            return;
        }

        if (!_controller.ShouldShowOverlay)
        {
            await HideRealtimeOverlayAsync(cancellationToken);
            return;
        }

        if (_showOcrPositionOnly)
        {
            await ShowOcrPositionPreviewAsync(recognized, frame.Region, stableText, isOneShot: false, cancellationToken);
            return;
        }

        var translation = await TranslateWithTelemetryAsync(recognized, translatableText, cancellationToken);
        if (IsStaleRealtimeFrame(frame))
        {
            return;
        }

        if (!_controller.ShouldShowOverlay)
        {
            await HideRealtimeOverlayAsync(cancellationToken);
            return;
        }

        await ShowOverlayAsync(
            translation.Text,
            translation.Telemetry.ProviderLabel,
            recognized.EstimatedTextHeightPixels,
            frame.Region,
            sourceTextBoundsPixels: recognized.TextBoundsPixels,
            sourceText: stableText,
            sourceTextLines: recognized.TextLines,
            isOneShot: false,
            cancellationToken: cancellationToken);
    }

    public async Task<TranslationLoopResult> TryTickAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            if (!_controller.ShouldCapture)
            {
                await HideRealtimeOverlayAsync(cancellationToken);
                return TranslationLoopResult.HiddenOverlay;
            }

            var captureStopwatch = Stopwatch.StartNew();
            var frame = await _capture.CaptureAsync(cancellationToken);
            captureStopwatch.Stop();

            var ocrStopwatch = Stopwatch.StartNew();
            var recognized = await RecognizeChangedOrCachedAsync(frame, cancellationToken);
            ocrStopwatch.Stop();
            if (recognized is null)
            {
                return TranslationLoopResult.NoChange;
            }

            if (IsStaleRealtimeFrame(frame))
            {
                return TranslationLoopResult.NoChange;
            }

            if (!_showOcrPositionOnly && IsLikelyVisibleRealtimeOverlayEcho(recognized.Text))
            {
                return TranslationLoopResult.NoChange;
            }

            if (!_showOcrPositionOnly && _controller.ShouldShowOverlay)
            {
                await ShowLocalPreviewAsync(recognized, frame.Region, recognized.Text, isOneShot: false, cancellationToken);
            }

            var stableText = _textStabilizer.Observe(recognized.Text);
            var stabilizationLatency = TrackStabilizationLatency(recognized.Text, stableText);
            if (stableText is null)
            {
                return TranslationLoopResult.NoChange;
            }

            _controller.AcceptStableSourceText(stableText);
            var translatableText = SourceTextTranslationExtractor.ExtractTranslatableText(stableText, _targetLanguage);
            if (!SourceTextTranslationFilter.ShouldTranslate(translatableText, _targetLanguage))
            {
                return TranslationLoopResult.SkippedNoEnglish;
            }

            if (!_controller.ShouldShowOverlay)
            {
                await HideRealtimeOverlayAsync(cancellationToken);
                return TranslationLoopResult.HiddenOverlay;
            }

            if (_showOcrPositionOnly)
            {
                await ShowOcrPositionPreviewAsync(recognized, frame.Region, stableText, isOneShot: false, cancellationToken);
                return new TranslationLoopResult(
                    TranslationLoopOutcome.DisplayedTranslation,
                    string.Empty,
                    ConnectionStatusMessage: "OCR位置测试");
            }

            var translation = await TranslateWithTelemetryAsync(recognized, translatableText, cancellationToken);
            if (IsStaleRealtimeFrame(frame))
            {
                return TranslationLoopResult.NoChange;
            }

            if (!_controller.ShouldShowOverlay)
            {
                await HideRealtimeOverlayAsync(cancellationToken);
                return TranslationLoopResult.HiddenOverlay;
            }

            var overlayStopwatch = Stopwatch.StartNew();
            await ShowOverlayAsync(
                translation.Text,
                translation.Telemetry.ProviderLabel,
                recognized.EstimatedTextHeightPixels,
                frame.Region,
                sourceTextBoundsPixels: recognized.TextBoundsPixels,
                sourceText: stableText,
                sourceTextLines: recognized.TextLines,
                isOneShot: false,
                cancellationToken: cancellationToken);
            overlayStopwatch.Stop();

            return TranslationLoopResult.Displayed(translation.Telemetry with
            {
                Pipeline = new TranslationPipelineTelemetry(
                    captureStopwatch.Elapsed,
                    ocrStopwatch.Elapsed,
                    stabilizationLatency,
                    translation.Telemetry.Latency,
                    overlayStopwatch.Elapsed)
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return TranslationLoopResult.NoChange;
        }
        catch (Exception exception)
        {
            await HideAfterFailureAsync(isOneShot: false, cancellationToken);
            return TranslationLoopResult.Failed(exception);
        }
    }

    public async Task HideRealtimeOverlayAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _hasVisibleRealtimeOverlay = false;
        _lastRealtimeOverlayText = null;
        _lastRealtimeProviderLabel = null;
        await _overlayPresenter.HideAsync(cancellationToken);
    }

    public async Task<bool> TryRestoreRealtimeOverlayAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_lastRealtimeOverlaySnapshot is not { } snapshot ||
            IsStaleRealtimeAnchor(snapshot.AnchorRegion))
        {
            return false;
        }

        await _overlayPresenter.ShowAsync(
            snapshot.TranslatedText,
            snapshot.ProviderLabel,
            snapshot.SourceTextHeightPixels,
            snapshot.AnchorRegion,
            snapshot.SourceTextBoundsPixels,
            snapshot.SourceText,
            snapshot.SourceTextLines,
            cancellationToken);
        _lastRealtimeOverlayText = snapshot.TranslatedText;
        _lastRealtimeProviderLabel = snapshot.ProviderLabel;
        _hasVisibleRealtimeOverlay = true;
        return true;
    }

    public async Task RunOneShotAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var frame = await _capture.CaptureAsync(cancellationToken);
        await ProcessOneShotFrameAsync(frame, anchorRegion: null, cancellationToken);
    }

    public async Task<TranslationLoopResult> TryRunOneShotAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            var frame = await _capture.CaptureAsync(cancellationToken);
            return await ProcessOneShotFrameAsync(frame, anchorRegion: null, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return TranslationLoopResult.NoChange;
        }
        catch (Exception exception)
        {
            await HideAfterFailureAsync(isOneShot: true, cancellationToken);
            return TranslationLoopResult.Failed(exception);
        }
    }

    public async Task<TranslationLoopResult> TryRunOneShotAsync(CapturedFrame frame, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            return await ProcessOneShotFrameAsync(frame, frame.Region, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return TranslationLoopResult.NoChange;
        }
        catch (Exception exception)
        {
            await HideAfterFailureAsync(isOneShot: true, cancellationToken);
            return TranslationLoopResult.Failed(exception);
        }
    }

    private async Task<TranslationLoopResult> ProcessOneShotFrameAsync(
        CapturedFrame frame,
        FrameRegion? anchorRegion,
        CancellationToken cancellationToken)
    {
        var recognized = await _ocrEngine.RecognizeAsync(frame, cancellationToken);
        var normalized = TextNormalizer.NormalizePreservingLineBreaks(recognized.Text);
        if (normalized.Length == 0)
        {
            await _overlayPresenter.HideOneShotAsync(cancellationToken);
            return TranslationLoopResult.HiddenOverlay;
        }

        var translatableText = SourceTextTranslationExtractor.ExtractTranslatableText(normalized, _targetLanguage);
        if (!SourceTextTranslationFilter.ShouldTranslate(translatableText, _targetLanguage))
        {
            await _overlayPresenter.HideOneShotAsync(cancellationToken);
            return TranslationLoopResult.SkippedNoEnglish;
        }

        if (_showOcrPositionOnly)
        {
            await ShowOcrPositionPreviewAsync(recognized, anchorRegion, normalized, isOneShot: true, cancellationToken);
            return new TranslationLoopResult(
                TranslationLoopOutcome.DisplayedTranslation,
                string.Empty,
                ConnectionStatusMessage: "OCR位置测试");
        }

        await ShowLocalPreviewAsync(recognized, anchorRegion, normalized, isOneShot: true, cancellationToken);
        var translation = await TranslateWithTelemetryAsync(recognized, translatableText, cancellationToken);
        await _overlayPresenter.ShowOneShotAsync(
            translation.Text,
            translation.Telemetry.ProviderLabel,
            recognized.EstimatedTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels: recognized.TextBoundsPixels,
            sourceText: normalized,
            sourceTextLines: recognized.TextLines,
            cancellationToken: cancellationToken);
        return TranslationLoopResult.Displayed(translation.Telemetry);
    }

    private async Task<OcrResult?> RecognizeChangedOrCachedAsync(
        CapturedFrame frame,
        CancellationToken cancellationToken)
    {
        var regionChanged = !_lastRecognizedRegion.HasValue ||
            !_lastRecognizedRegion.Value.Equals(frame.Region);
        var hasMeaningfulChange = _frameChangeDetector.HasMeaningfulChange(frame);
        if (!regionChanged && !hasMeaningfulChange)
        {
            return _lastRecognized;
        }

        _lastRecognized = await _ocrEngine.RecognizeAsync(frame, cancellationToken);
        _lastRecognizedRegion = frame.Region;
        return _lastRecognized;
    }

    private Task ShowOcrPositionPreviewAsync(
        OcrResult recognized,
        FrameRegion? anchorRegion,
        string sourceText,
        bool isOneShot,
        CancellationToken cancellationToken)
    {
        var previewText = BuildOcrPositionPreviewText(recognized.TextLines, sourceText);
        return ShowOverlayAsync(
            previewText,
            "OCR",
            recognized.EstimatedTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels: recognized.TextBoundsPixels,
            sourceText: sourceText,
            sourceTextLines: recognized.TextLines,
            isOneShot: isOneShot,
            cancellationToken: cancellationToken);
    }

    private string BuildOcrPositionPreviewText(
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        string fallbackText)
    {
        var lineTexts = OcrLineTextExtractor.GetTranslatableBoundedLineTexts(sourceTextLines, _targetLanguage);

        return lineTexts.Length > 0
            ? string.Join('\n', lineTexts)
            : SourceTextTranslationExtractor.ExtractTranslatableText(fallbackText, _targetLanguage);
    }

    private async Task ShowLocalPreviewAsync(
        OcrResult recognized,
        FrameRegion? anchorRegion,
        string sourceText,
        bool isOneShot,
        CancellationToken cancellationToken)
    {
        if (!_localPreviewEnabled ||
            _targetLanguage != TargetLanguage.SimplifiedChinese ||
            _localPreviewProvider is null)
        {
            return;
        }

        var previewSourceText = TextNormalizer.NormalizePreservingLineBreaks(sourceText);
        if (previewSourceText.Length == 0 ||
            string.Equals(_lastLocalPreviewSourceText, previewSourceText, StringComparison.Ordinal))
        {
            return;
        }

        string? previewText;
        try
        {
            previewText = await _localPreviewProvider.BuildPreviewAsync(
                recognized.TextLines,
                sourceText,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return;
        }

        if (previewText is null)
        {
            return;
        }

        _lastLocalPreviewSourceText = previewSourceText;
        await ShowOverlayAsync(
            previewText,
            _localPreviewProvider.ProviderLabel,
            recognized.EstimatedTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels: recognized.TextBoundsPixels,
            sourceText: sourceText,
            sourceTextLines: recognized.TextLines,
            isOneShot: isOneShot,
            cancellationToken: cancellationToken);
    }

    private async Task ShowOverlayAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        string? sourceText,
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        bool isOneShot,
        CancellationToken cancellationToken)
    {
        if (isOneShot)
        {
            await _overlayPresenter.ShowOneShotAsync(
                translatedText,
                providerLabel,
                sourceTextHeightPixels,
                anchorRegion,
                sourceTextBoundsPixels,
                sourceText,
                sourceTextLines,
            cancellationToken);
        }
        else
        {
            if (IsStaleRealtimeAnchor(anchorRegion))
            {
                return;
            }

            await _overlayPresenter.ShowAsync(
                translatedText,
                providerLabel,
                sourceTextHeightPixels,
                anchorRegion,
                sourceTextBoundsPixels,
                sourceText,
                sourceTextLines,
                cancellationToken);
            _lastRealtimeOverlaySnapshot = new RealtimeOverlaySnapshot(
                translatedText,
                providerLabel,
                sourceTextHeightPixels,
                anchorRegion,
                sourceTextBoundsPixels,
                sourceText,
                sourceTextLines);
            _lastRealtimeOverlayText = translatedText;
            _lastRealtimeProviderLabel = providerLabel;
            _hasVisibleRealtimeOverlay = true;
        }
    }

    private async Task<TranslationResult> TranslateWithTelemetryAsync(
        OcrResult recognized,
        string sourceText,
        CancellationToken cancellationToken)
    {
        var requestCountBefore = _translationRequestCount;
        var stopwatch = Stopwatch.StartNew();
        var translated = _alignTranslationToOcrLines
            ? await TranslateOcrLinesAsync(recognized.TextLines, sourceText, cancellationToken)
            : await TranslateAsync(sourceText, cancellationToken);
        stopwatch.Stop();
        return new TranslationResult(
            translated,
            new TranslationTelemetry(
                ReadProviderLabel(),
                stopwatch.Elapsed,
                RequestCount: Math.Max(0, _translationRequestCount - requestCountBefore)));
    }

    private async Task<string> TranslateOcrLinesAsync(
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        string fallbackSourceText,
        CancellationToken cancellationToken)
    {
        var lineTexts = OcrLineTextExtractor.GetTranslatableBoundedLineTexts(sourceTextLines, _targetLanguage);
        if (lineTexts.Length <= 1)
        {
            return await TranslateAsync(fallbackSourceText, cancellationToken);
        }

        var batchTranslated = await TranslateAsync(
            NumberedBatchTranslationFormatter.BuildPrompt(lineTexts, _targetLanguage),
            cancellationToken);
        if (NumberedBatchTranslationFormatter.TryParse(batchTranslated, lineTexts.Length, out var batchTranslatedLines))
        {
            return string.Join('\n', batchTranslatedLines);
        }

        var translatedLines = new string[lineTexts.Length];
        for (var index = 0; index < lineTexts.Length; index++)
        {
            translatedLines[index] = NumberedBatchTranslationFormatter.NormalizeTranslatedLine(
                await TranslateAsync(lineTexts[index], cancellationToken));
        }

        return string.Join('\n', translatedLines);
    }

    private async Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        if (_translationCache.TryGet(sourceText, out var translated))
        {
            return translated;
        }

        _translationRequestCount++;
        translated = await _translationProvider.TranslateAsync(sourceText, cancellationToken);
        translated = TranslationTextNormalizer.NormalizeForDisplay(translated);
        _translationCache.Store(sourceText, translated);
        return translated;
    }


    private TimeSpan TrackStabilizationLatency(string recognizedText, string? stableText)
    {
        var normalized = TextNormalizer.NormalizePreservingLineBreaks(recognizedText);
        if (normalized.Length == 0)
        {
            return TimeSpan.Zero;
        }

        var now = Stopwatch.GetTimestamp();
        if (!string.Equals(_stabilizationCandidateText, normalized, StringComparison.Ordinal))
        {
            _stabilizationCandidateText = normalized;
            _stabilizationCandidateFirstSeenTimestamp = now;
        }

        return stableText is null
            ? TimeSpan.Zero
            : Stopwatch.GetElapsedTime(_stabilizationCandidateFirstSeenTimestamp, now);
    }

    private string ReadProviderLabel()
    {
        return _translationProvider is ITranslationProviderMetadata metadata
            ? metadata.ProviderLabel
            : string.Empty;
    }

    private bool IsStaleRealtimeFrame(CapturedFrame frame)
    {
        return _capture.CurrentRegion is { } currentRegion &&
            !currentRegion.Equals(frame.Region);
    }

    private bool IsStaleRealtimeAnchor(FrameRegion? anchorRegion)
    {
        return anchorRegion.HasValue &&
            _capture.CurrentRegion is { } currentRegion &&
            !currentRegion.Equals(anchorRegion.Value);
    }

    private bool IsLikelyVisibleRealtimeOverlayEcho(string recognizedText)
    {
        if (!_hasVisibleRealtimeOverlay)
        {
            return false;
        }

        var normalized = TextNormalizer.NormalizePreservingLineBreaks(recognizedText);
        if (normalized.Length == 0)
        {
            return false;
        }

        return ContainsEchoText(normalized, _lastRealtimeOverlayText) ||
            ContainsEchoText(normalized, _lastRealtimeProviderLabel) ||
            SharesEnoughCjkCharacters(normalized, _lastRealtimeOverlayText);
    }

    private static bool ContainsEchoText(string recognizedText, string? overlayText)
    {
        var recognized = CompactForEchoComparison(recognizedText);
        var overlay = CompactForEchoComparison(overlayText);
        return overlay.Length >= 3 &&
            recognized.Contains(overlay, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SharesEnoughCjkCharacters(string recognizedText, string? overlayText)
    {
        if (string.IsNullOrWhiteSpace(overlayText))
        {
            return false;
        }

        var overlayCharacters = overlayText
            .Where(IsCjkLike)
            .Distinct()
            .ToArray();
        if (overlayCharacters.Length < 4)
        {
            return false;
        }

        var recognizedCharacters = recognizedText
            .Where(IsCjkLike)
            .ToHashSet();
        if (recognizedCharacters.Count < 4)
        {
            return false;
        }

        var shared = overlayCharacters.Count(recognizedCharacters.Contains);
        return shared >= 4 && shared >= Math.Ceiling(overlayCharacters.Length * 0.45);
    }

    private static string CompactForEchoComparison(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return new string(TextNormalizer
            .NormalizePreservingLineBreaks(text)
            .Where(value => !char.IsWhiteSpace(value))
            .ToArray());
    }

    private static bool IsCjkLike(char value)
    {
        return value is
            >= '\u3400' and <= '\u4DBF' or
            >= '\u4E00' and <= '\u9FFF' or
            >= '\uF900' and <= '\uFAFF' or
            >= '\u3040' and <= '\u30FF' or
            >= '\uAC00' and <= '\uD7AF';
    }

    private async Task HideAfterFailureAsync(bool isOneShot, CancellationToken cancellationToken)
    {
        try
        {
            if (isOneShot)
            {
                await _overlayPresenter.HideOneShotAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ocrEngine is IDisposable disposableOcrEngine)
        {
            disposableOcrEngine.Dispose();
        }
    }

    private sealed record TranslationResult(string Text, TranslationTelemetry Telemetry);

    private sealed record RealtimeOverlaySnapshot(
        string TranslatedText,
        string ProviderLabel,
        double? SourceTextHeightPixels,
        FrameRegion? AnchorRegion,
        FrameRegion? SourceTextBoundsPixels,
        string? SourceText,
        IReadOnlyList<OcrTextLine>? SourceTextLines);
}
