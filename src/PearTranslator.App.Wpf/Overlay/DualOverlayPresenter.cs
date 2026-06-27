using System.Windows.Threading;
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Presentation;

namespace PearTranslator.App.Wpf.Overlay;

public sealed class DualOverlayPresenter : IOverlayPresenter, IDisposable
{
    private readonly OverlayWindow _realtimeWindow;
    private readonly OverlayWindow _oneShotWindow;
    private readonly SelectionMarkerWindow _realtimeSelectionMarker;
    private readonly SelectionMarkerWindow _oneShotSelectionMarker;
    private readonly DispatcherTimer _oneShotTimer;
    private FrameRegion? _lastRealtimeRegion;
    private FrameRegion? _activeOneShotRegion;
    private bool _isOneShotVisible;
    private bool _selectedRegionMarkerEnabled;
    private int _oneShotDisplaySeconds;

    public DualOverlayPresenter(
        OverlayWindow realtimeWindow,
        OverlayWindow oneShotWindow,
        int oneShotDisplaySeconds)
    {
        _realtimeWindow = realtimeWindow;
        _oneShotWindow = oneShotWindow;
        _oneShotDisplaySeconds = NormalizeDisplaySeconds(oneShotDisplaySeconds);
        _oneShotWindow.OneShotCloseRequested += OnOneShotCloseRequested;
        _realtimeWindow.SelectedRegionMarkerToggled += OnSelectedRegionMarkerToggled;
        _oneShotWindow.SelectedRegionMarkerToggled += OnSelectedRegionMarkerToggled;
        _realtimeSelectionMarker = new SelectionMarkerWindow();
        _oneShotSelectionMarker = new SelectionMarkerWindow();
        _oneShotTimer = new DispatcherTimer();
        _oneShotTimer.Tick += (_, _) => HideOneShotOverlay();
    }

    public bool PositionOverlayEnabled
    {
        get => _realtimeWindow.PositionOverlayEnabled;
        set
        {
            _realtimeWindow.PositionOverlayEnabled = value;
            _oneShotWindow.PositionOverlayEnabled = value;
        }
    }

    public bool ExcludeOverlayFromCapture
    {
        get => _realtimeWindow.ExcludeFromCaptureEnabled;
        set
        {
            _realtimeWindow.ExcludeFromCaptureEnabled = value;
            _oneShotWindow.ExcludeFromCaptureEnabled = value;
            _realtimeSelectionMarker.ExcludeFromCaptureEnabled = value;
            _oneShotSelectionMarker.ExcludeFromCaptureEnabled = value;
        }
    }

    public TargetLanguage TargetLanguage
    {
        get => _realtimeWindow.TargetLanguage;
        set
        {
            _realtimeWindow.TargetLanguage = value;
            _oneShotWindow.TargetLanguage = value;
        }
    }

    public UiLanguage UiLanguage
    {
        get => _realtimeWindow.UiLanguage;
        set
        {
            _realtimeWindow.UiLanguage = value;
            _oneShotWindow.UiLanguage = value;
        }
    }

    public void UpdateOneShotDisplaySeconds(int seconds)
    {
        _oneShotDisplaySeconds = NormalizeDisplaySeconds(seconds);
        if (_isOneShotVisible)
        {
            RestartOneShotTimer();
        }
    }

    public void PrepareOneShotSelection()
    {
        HideOneShotOverlay();
    }

    public void ShowRealtimePending(FrameRegion region)
    {
        _lastRealtimeRegion = region;
        if (IsRealtimeBlockedByOneShot(region))
        {
            _ = _realtimeWindow.HideAsync(CancellationToken.None);
            _realtimeSelectionMarker.Hide();
            return;
        }

        _realtimeWindow.ShowPending(region);
        ShowRealtimeSelectionMarker(region);
    }

    public Task ShowAsync(string translatedText, CancellationToken cancellationToken)
    {
        return ShowAsync(translatedText, string.Empty, cancellationToken);
    }

    public Task ShowAsync(string translatedText, string providerLabel, CancellationToken cancellationToken)
    {
        return ShowAsync(translatedText, providerLabel, null, cancellationToken);
    }

    public Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        CancellationToken cancellationToken)
    {
        return ShowAsync(translatedText, providerLabel, sourceTextHeightPixels, null, cancellationToken);
    }

    public Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        CancellationToken cancellationToken)
    {
        return ShowAsync(
            translatedText,
            providerLabel,
            sourceTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels: null,
            cancellationToken);
    }

    public Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        CancellationToken cancellationToken)
    {
        return ShowAsync(
            translatedText,
            providerLabel,
            sourceTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels,
            sourceText: null,
            cancellationToken);
    }

    public Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        string? sourceText,
        CancellationToken cancellationToken)
    {
        return ShowAsync(
            translatedText,
            providerLabel,
            sourceTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels,
            sourceText,
            sourceTextLines: null,
            cancellationToken);
    }

    public async Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        string? sourceText,
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        CancellationToken cancellationToken)
    {
        _lastRealtimeRegion = anchorRegion;
        if (IsRealtimeBlockedByOneShot(anchorRegion))
        {
            await _realtimeWindow.HideAsync(cancellationToken);
            _realtimeSelectionMarker.Hide();
            return;
        }

        await _realtimeWindow.ShowAsync(
            translatedText,
            providerLabel,
            sourceTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels,
            sourceText,
            sourceTextLines,
            cancellationToken);
        ShowRealtimeSelectionMarker(anchorRegion);
    }

    public async Task ShowOneShotAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        string? sourceText,
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        CancellationToken cancellationToken)
    {
        _activeOneShotRegion = anchorRegion;
        _isOneShotVisible = true;
        if (IsRealtimeBlockedByOneShot(_lastRealtimeRegion))
        {
            await _realtimeWindow.HideAsync(cancellationToken);
            _realtimeSelectionMarker.Hide();
        }

        await _oneShotWindow.ShowOneShotAsync(
            translatedText,
            providerLabel,
            sourceTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels,
            sourceText,
            sourceTextLines,
            cancellationToken);
        ShowOneShotSelectionMarker(anchorRegion);
        RestartOneShotTimer();
    }

    public Task HideAsync(CancellationToken cancellationToken)
    {
        _lastRealtimeRegion = null;
        _realtimeSelectionMarker.Hide();
        return _realtimeWindow.HideAsync(cancellationToken);
    }

    public Task HideOneShotAsync(CancellationToken cancellationToken)
    {
        HideOneShotOverlay();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _oneShotTimer.Stop();
        _oneShotWindow.OneShotCloseRequested -= OnOneShotCloseRequested;
        _realtimeWindow.SelectedRegionMarkerToggled -= OnSelectedRegionMarkerToggled;
        _oneShotWindow.SelectedRegionMarkerToggled -= OnSelectedRegionMarkerToggled;
        _realtimeSelectionMarker.Close();
        _oneShotSelectionMarker.Close();
    }

    private bool IsRealtimeBlockedByOneShot(FrameRegion? realtimeRegion)
    {
        return _isOneShotVisible &&
            realtimeRegion.HasValue &&
            _activeOneShotRegion.HasValue &&
            OverlayRegionPolicy.Overlaps(realtimeRegion.Value, _activeOneShotRegion.Value);
    }

    private void RestartOneShotTimer()
    {
        _oneShotTimer.Stop();
        if (_oneShotDisplaySeconds <= 0)
        {
            return;
        }

        _oneShotTimer.Interval = TimeSpan.FromSeconds(_oneShotDisplaySeconds);
        _oneShotTimer.Start();
    }

    private void HideOneShotOverlay()
    {
        _oneShotTimer.Stop();
        _isOneShotVisible = false;
        _activeOneShotRegion = null;
        _oneShotSelectionMarker.Hide();
        _ = _oneShotWindow.HideAsync(CancellationToken.None);
        ShowRealtimeSelectionMarker(_lastRealtimeRegion);
    }

    private void OnOneShotCloseRequested(object? sender, EventArgs e)
    {
        HideOneShotOverlay();
    }

    private void OnSelectedRegionMarkerToggled(object? sender, bool isEnabled)
    {
        _selectedRegionMarkerEnabled = isEnabled;
        ApplySelectedRegionMarkerState();
    }

    private void ApplySelectedRegionMarkerState()
    {
        _realtimeWindow.SetSelectedRegionMarkerEnabled(_selectedRegionMarkerEnabled);
        _oneShotWindow.SetSelectedRegionMarkerEnabled(_selectedRegionMarkerEnabled);

        if (!_selectedRegionMarkerEnabled)
        {
            _realtimeSelectionMarker.Hide();
            _oneShotSelectionMarker.Hide();
            return;
        }

        ShowRealtimeSelectionMarker(_lastRealtimeRegion);
        ShowOneShotSelectionMarker(_activeOneShotRegion);
    }

    private void ShowRealtimeSelectionMarker(FrameRegion? region)
    {
        if (!_selectedRegionMarkerEnabled || !region.HasValue || IsRealtimeBlockedByOneShot(region))
        {
            _realtimeSelectionMarker.Hide();
            return;
        }

        _realtimeSelectionMarker.ShowRegion(region.Value);
    }

    private void ShowOneShotSelectionMarker(FrameRegion? region)
    {
        if (!_selectedRegionMarkerEnabled || !_isOneShotVisible || !region.HasValue)
        {
            _oneShotSelectionMarker.Hide();
            return;
        }

        _oneShotSelectionMarker.ShowRegion(region.Value);
    }

    private static int NormalizeDisplaySeconds(int seconds)
    {
        return Math.Max(0, seconds);
    }
}
