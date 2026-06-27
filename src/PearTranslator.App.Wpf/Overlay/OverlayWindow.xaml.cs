using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Presentation;
using PearTranslator.Core.Text;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfPanel = System.Windows.Controls.Panel;
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace PearTranslator.App.Wpf.Overlay;

public partial class OverlayWindow : Window, IOverlayPresenter
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolwindow = 0x00000080;
    private const int WsExNoactivate = 0x08000000;
    private const int WmNchittest = 0x0084;
    private const int HtTransparent = -1;
    private const uint WdaNone = 0x00000000;
    private const uint WdaExcludeFromCapture = 0x00000011;
    private const uint WdaMonitor = 0x00000001;
    private const double MinimumTextWidth = 80;
    private const double ToolbarHeight = 20;
    private const string PendingCaptionText = "\u8bc6\u522b\u4e2d...";
    private const string PendingProviderLabel = "OCR";
    private static readonly bool ShowDebugMarkers = false;
    private static readonly WpfBrush DisplayBackgroundBrush = new SolidColorBrush(WpfColor.FromArgb(0xD0, 0x20, 0x20, 0x24));
    private static readonly WpfBrush SourceRegionBackgroundBrush = new SolidColorBrush(WpfColor.FromArgb(0xD0, 0x20, 0x20, 0x24));
    private static readonly DropShadowEffect CaptionTextShadowEffect = CreateCaptionTextShadow();
    private readonly DispatcherTimer _clickThroughTimer;
    private readonly WindowDisplayAffinityState _displayAffinityState = new();
    private readonly List<OutlinedTextBlock> _captionLineTextBlocks = [];
    private readonly List<WpfRectangle> _sourceRegionBackgroundRectangles = [];
    private readonly List<WpfRectangle> _sourceLineDebugRectangles = [];
    private FrameRegion? _anchorRegion;
    private FrameRegion? _lastSourceTextBoundsPixels;
    private string? _lastSourceText;
    private IReadOnlyList<OcrTextLine>? _lastSourceTextLines;
    private bool _isClickThroughEnabled;
    private bool _usesManualPosition;
    private bool _captionOutlineEnabled = true;
    private bool _selectedRegionMarkerEnabled;
    private OverlayBackgroundMode _backgroundMode = OverlayBackgroundMode.Full;
    private bool _positionOverlayEnabled;
    private bool _excludeFromCaptureEnabled = true;
    private TargetLanguage _targetLanguage = TargetLanguage.SimplifiedChinese;
    private UiLanguage _uiLanguage = UiLanguage.SimplifiedChinese;

    public OverlayWindow()
    {
        InitializeComponent();
        ApplyCaptionTextEffects();
        UpdateLocalizedTooltips();
        UpdateCaptionOutlineButtonState();
        UpdateSelectedRegionMarkerButtonState();
        _clickThroughTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _clickThroughTimer.Tick += (_, _) => UpdateClickThroughMode();
        SourceInitialized += (_, _) => ConfigureWindowInterop();
    }

    public event EventHandler? OneShotCloseRequested;
    public event EventHandler<bool>? SelectedRegionMarkerToggled;

    public UiLanguage UiLanguage
    {
        get => _uiLanguage;
        set
        {
            if (_uiLanguage == value)
            {
                return;
            }

            _uiLanguage = value;
            UpdateLocalizedTooltips();
            UpdateCaptionOutlineButtonState();
            UpdateSelectedRegionMarkerButtonState();
        }
    }

    public TargetLanguage TargetLanguage
    {
        get => _targetLanguage;
        set
        {
            if (_targetLanguage == value)
            {
                return;
            }

            _targetLanguage = value;
            if (IsVisible)
            {
                ConfigureCaptionLayout(
                    anchorRegion: null,
                    sourceTextBoundsPixels: _lastSourceTextBoundsPixels,
                    sourceText: _lastSourceText,
                    sourceTextLines: _lastSourceTextLines);
            }
        }
    }

    public bool PositionOverlayEnabled
    {
        get => _positionOverlayEnabled;
        set
        {
            _positionOverlayEnabled = value;
            if (IsVisible)
            {
                ConfigureCaptionLayout(
                    anchorRegion: null,
                    sourceTextBoundsPixels: _lastSourceTextBoundsPixels,
                    sourceText: _lastSourceText,
                    sourceTextLines: _lastSourceTextLines);
            }
        }
    }

    public bool ExcludeFromCaptureEnabled
    {
        get => _excludeFromCaptureEnabled;
        set
        {
            if (_excludeFromCaptureEnabled == value)
            {
                return;
            }

            _excludeFromCaptureEnabled = value;
            ApplyCaptureAffinityForCurrentState();
        }
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
            sourceTextLines: null,
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

    public Task ShowAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        string? sourceText,
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        CancellationToken cancellationToken)
    {
        return ShowCoreAsync(
            translatedText,
            providerLabel,
            sourceTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels,
            sourceText,
            sourceTextLines,
            isOneShot: false,
            cancellationToken);
    }

    public Task ShowOneShotAsync(
        string translatedText,
        string providerLabel,
        double? sourceTextHeightPixels,
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        string? sourceText,
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        CancellationToken cancellationToken)
    {
        return ShowCoreAsync(
            translatedText,
            providerLabel,
            sourceTextHeightPixels,
            anchorRegion,
            sourceTextBoundsPixels,
            sourceText,
            sourceTextLines,
            isOneShot: true,
            cancellationToken);
    }

    private Task ShowCoreAsync(
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
        Dispatcher.Invoke(() =>
        {
            ApplyCaptionFontSize(sourceTextHeightPixels);
            CaptionText.Text = translatedText;
            ProviderLabelText.Text = providerLabel;
            CloseOneShotOverlayButton.Visibility = isOneShot ? Visibility.Visible : Visibility.Collapsed;
            ProviderLabelText.Visibility = string.IsNullOrWhiteSpace(providerLabel)
                ? Visibility.Collapsed
                : Visibility.Visible;
            _lastSourceTextBoundsPixels = sourceTextBoundsPixels;
            _lastSourceText = sourceText;
            _lastSourceTextLines = sourceTextLines;
            if (!IsVisible)
            {
                Show();
            }

            ApplyCaptureAffinityForCurrentState();
            ConfigureCaptionLayout(anchorRegion, sourceTextBoundsPixels, sourceText, sourceTextLines);
            if (_usesManualPosition)
            {
                return;
            }

            PositionAtAnchor(anchorRegion);
        });

        return Task.CompletedTask;
    }

    public void ShowPending(FrameRegion region)
    {
        Dispatcher.Invoke(() =>
        {
            _anchorRegion = region;
            _usesManualPosition = false;
            _lastSourceTextBoundsPixels = null;
            _lastSourceText = null;
            _lastSourceTextLines = null;
            CloseOneShotOverlayButton.Visibility = Visibility.Collapsed;

            ApplyCaptionFontSize(sourceTextHeightPixels: null);
            CaptionText.Text = PendingCaptionText;
            ProviderLabelText.Text = PendingProviderLabel;
            ProviderLabelText.Visibility = Visibility.Visible;

            if (!IsVisible)
            {
                Show();
            }

            ApplyCaptureAffinityForCurrentState();
            ConfigureCaptionLayout(region, sourceTextBoundsPixels: null, sourceText: null, sourceTextLines: null);
            PositionAtAnchor(region);
        });
    }

    public Task HideAsync(CancellationToken cancellationToken)
    {
        Dispatcher.Invoke(() =>
        {
            CloseOneShotOverlayButton.Visibility = Visibility.Collapsed;
            Hide();
            ClearCaptureExclusion();
        });
        return Task.CompletedTask;
    }

    public void SetAnchorRegion(FrameRegion region)
    {
        _anchorRegion = region;
        _usesManualPosition = false;
        if (IsVisible)
        {
            ConfigureCaptionLayout(
                anchorRegion: null,
                sourceTextBoundsPixels: null,
                sourceText: null,
                sourceTextLines: null);
            PositionAtAnchor(anchorRegion: null);
        }
    }

    private void OnDragOverlayButtonMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        try
        {
            DragMove();
            _usesManualPosition = true;
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void OnToggleOverlayBackgroundClicked(object sender, RoutedEventArgs e)
    {
        _backgroundMode = OverlayBackgroundModeCycler.Next(_backgroundMode);
        ApplyBackgroundMode();
        if (IsVisible)
        {
            ConfigureCaptionLayout(
                anchorRegion: null,
                sourceTextBoundsPixels: _lastSourceTextBoundsPixels,
                sourceText: _lastSourceText,
                sourceTextLines: _lastSourceTextLines);
        }
    }

    private void OnToggleCaptionOutlineClicked(object sender, RoutedEventArgs e)
    {
        _captionOutlineEnabled = !_captionOutlineEnabled;
        ApplyCaptionTextEffects();
        UpdateCaptionOutlineButtonState();
    }

    private void OnToggleSelectedRegionMarkerClicked(object sender, RoutedEventArgs e)
    {
        _selectedRegionMarkerEnabled = !_selectedRegionMarkerEnabled;
        UpdateSelectedRegionMarkerButtonState();
        SelectedRegionMarkerToggled?.Invoke(this, _selectedRegionMarkerEnabled);
    }

    public void SetSelectedRegionMarkerEnabled(bool isEnabled)
    {
        if (_selectedRegionMarkerEnabled == isEnabled)
        {
            return;
        }

        _selectedRegionMarkerEnabled = isEnabled;
        UpdateSelectedRegionMarkerButtonState();
    }

    private void OnCloseOneShotOverlayClicked(object sender, RoutedEventArgs e)
    {
        CloseOneShotOverlayButton.Visibility = Visibility.Collapsed;
        Hide();
        ClearCaptureExclusion();
        OneShotCloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ConfigureWindowInterop()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var currentStyle = GetWindowLong(handle, GwlExstyle);
        SetWindowLong(handle, GwlExstyle, currentStyle | WsExToolwindow | WsExNoactivate);
        ApplyCaptureAffinityForCurrentState(handle);

        HwndSource.FromHwnd(handle)?.AddHook(WndProc);
        _clickThroughTimer.Start();
        SetClickThroughMode(isEnabled: true);
    }

    private void ApplyCaptureAffinityForCurrentState()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            ApplyCaptureAffinityForCurrentState(handle);
        }
    }

    private void ApplyCaptureAffinityForCurrentState(IntPtr handle)
    {
        var requestedMode = _excludeFromCaptureEnabled && IsVisible
            ? WindowDisplayAffinityMode.ExcludedFromCapture
            : WindowDisplayAffinityMode.None;

        ApplyCaptureAffinityMode(handle, requestedMode);
    }

    private void ApplyCaptureAffinityMode(IntPtr handle, WindowDisplayAffinityMode requestedMode)
    {
        if (!_displayAffinityState.TryMarkRequested(requestedMode))
        {
            return;
        }

        if (requestedMode == WindowDisplayAffinityMode.ExcludedFromCapture)
        {
            ApplyCaptureExclusion(handle);
            return;
        }

        ClearCaptureExclusionNative(handle);
    }

    private void ClearCaptureExclusion()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            ApplyCaptureAffinityMode(handle, WindowDisplayAffinityMode.None);
        }
    }

    private static void ApplyCaptureExclusion(IntPtr handle)
    {
        if (SetWindowDisplayAffinity(handle, WdaExcludeFromCapture))
        {
            return;
        }

        SetWindowDisplayAffinity(handle, WdaMonitor);
    }

    private static void ClearCaptureExclusionNative(IntPtr handle)
    {
        SetWindowDisplayAffinity(handle, WdaNone);
    }

    private IntPtr WndProc(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (msg == WmNchittest && !IsPointInsideOverlayControls(lParam))
        {
            handled = true;
            return new IntPtr(HtTransparent);
        }

        return IntPtr.Zero;
    }

    private bool IsPointInsideOverlayControls(IntPtr lParam)
    {
        if (!IsLoaded)
        {
            return false;
        }

        var packed = lParam.ToInt64();
        var screenPoint = new System.Windows.Point(
            unchecked((short)(packed & 0xFFFF)),
            unchecked((short)((packed >> 16) & 0xFFFF)));
        var localPoint = OverlayControlsHost.PointFromScreen(screenPoint);

        return localPoint.X >= 0
            && localPoint.Y >= 0
            && localPoint.X <= OverlayControlsHost.ActualWidth
            && localPoint.Y <= OverlayControlsHost.ActualHeight;
    }

    private void UpdateClickThroughMode()
    {
        if (!IsVisible || !IsLoaded)
        {
            return;
        }

        if (!GetCursorPos(out var point))
        {
            return;
        }

        var screenPoint = new System.Windows.Point(point.X, point.Y);
        var localPoint = OverlayControlsHost.PointFromScreen(screenPoint);
        var isOverControls = localPoint.X >= 0
            && localPoint.Y >= 0
            && localPoint.X <= OverlayControlsHost.ActualWidth
            && localPoint.Y <= OverlayControlsHost.ActualHeight;

        SetClickThroughMode(!isOverControls);
    }

    private void SetClickThroughMode(bool isEnabled)
    {
        if (_isClickThroughEnabled == isEnabled)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var currentStyle = GetWindowLong(handle, GwlExstyle);
        var nextStyle = isEnabled
            ? currentStyle | WsExTransparent
            : currentStyle & ~WsExTransparent;
        SetWindowLong(handle, GwlExstyle, nextStyle);
        _isClickThroughEnabled = isEnabled;
    }

    private void PositionAtAnchor(FrameRegion? anchorRegion)
    {
        if ((anchorRegion ?? _anchorRegion) is not { } region)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var regionLeft = region.X / dpi.DpiScaleX;
        var regionTop = (region.Y / dpi.DpiScaleY) - ToolbarHeight;

        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        Left = OverlayLayoutCalculator.ClampWindowOrigin(
            regionLeft,
            virtualLeft,
            SystemParameters.VirtualScreenWidth,
            Width);
        Top = OverlayLayoutCalculator.ClampWindowOrigin(
            regionTop,
            virtualTop,
            SystemParameters.VirtualScreenHeight,
            Height);
    }

    private void ConfigureCaptionLayout(
        FrameRegion? anchorRegion,
        FrameRegion? sourceTextBoundsPixels,
        string? sourceText,
        IReadOnlyList<OcrTextLine>? sourceTextLines)
    {
        ClearCaptionLineTextBlocks();
        ClearSourceRegionBackgroundRectangles();
        ClearSourceLineDebugRectangles();
        CaptionText.Visibility = Visibility.Visible;
        ApplyBackgroundMode();

        if ((anchorRegion ?? _anchorRegion) is not { } region)
        {
            PositionCaptionFallback();
            HideDebugMarkers();
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var layout = OverlayLayoutCalculator.Calculate(region, dpi.DpiScaleX, dpi.DpiScaleY, ToolbarHeight);
        Width = layout.WindowWidth;
        Height = layout.WindowHeight;
        CaptionCanvas.Width = layout.CanvasWidth;
        CaptionCanvas.Height = layout.CanvasHeight;
        PositionSourceRegionBackgrounds(sourceTextLines, sourceTextBoundsPixels, layout, dpi.DpiScaleX, dpi.DpiScaleY);

        if (_positionOverlayEnabled &&
            TryPositionCaptionLinesFromSourceLines(
                sourceTextLines,
                layout,
                dpi.DpiScaleX,
                dpi.DpiScaleY))
        {
            HideSourceTextDebugMarker();
            PositionSourceLineDebugMarkers(sourceTextLines, layout, dpi.DpiScaleX, dpi.DpiScaleY);
            return;
        }

        if (_positionOverlayEnabled && sourceTextBoundsPixels is { } bounds)
        {
            PositionCaptionFromSourceBounds(bounds, layout, dpi.DpiScaleX, dpi.DpiScaleY);
            PositionSourceTextDebugMarker(bounds, layout, dpi.DpiScaleX, dpi.DpiScaleY, sourceText);
        }
        else
        {
            PositionCaptionFallback();
            if (sourceTextBoundsPixels is { } debugBounds)
            {
                PositionSourceTextDebugMarker(debugBounds, layout, dpi.DpiScaleX, dpi.DpiScaleY, sourceText);
            }
            else
            {
                HideSourceTextDebugMarker();
            }
        }
    }

    private void PositionCaptionFromSourceBounds(
        FrameRegion bounds,
        OverlayLayout layout,
        double dpiScaleX,
        double dpiScaleY)
    {
        var placement = OverlayLayoutCalculator.CalculateTextPlacement(
            bounds,
            layout,
            dpiScaleX,
            dpiScaleY,
            MinimumTextWidth);

        CaptionText.Width = placement.TextWidth;
        CaptionText.TextAlignment = TextAlignment.Left;
        Canvas.SetLeft(CaptionText, placement.Left);
        Canvas.SetTop(
            CaptionText,
            OverlayLayoutCalculator.CalculateCaptionTop(placement, layout, CaptionText.FontSize));
    }

    private void ApplyBackgroundMode()
    {
        DisplayBackgroundHost.Background = _backgroundMode == OverlayBackgroundMode.Full
            ? DisplayBackgroundBrush
            : System.Windows.Media.Brushes.Transparent;
    }

    private void PositionSourceRegionBackgrounds(
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        FrameRegion? sourceTextBoundsPixels,
        OverlayLayout layout,
        double dpiScaleX,
        double dpiScaleY)
    {
        if (_backgroundMode != OverlayBackgroundMode.SourceRegions)
        {
            return;
        }

        var sourceLines = GetTranslatableBoundedSourceLines(sourceTextLines);
        if (sourceLines.Length > 0)
        {
            foreach (var line in sourceLines)
            {
                AddSourceRegionBackground(line.BoundsPixels!.Value, layout, dpiScaleX, dpiScaleY);
            }

            return;
        }

        if (sourceTextBoundsPixels is { } bounds)
        {
            AddSourceRegionBackground(bounds, layout, dpiScaleX, dpiScaleY);
        }
    }

    private void AddSourceRegionBackground(
        FrameRegion bounds,
        OverlayLayout layout,
        double dpiScaleX,
        double dpiScaleY)
    {
        var placement = OverlayLayoutCalculator.CalculateTextPlacement(
            bounds,
            layout,
            dpiScaleX,
            dpiScaleY,
            minimumTextWidth: 1);
        var rectangle = new WpfRectangle
        {
            Fill = SourceRegionBackgroundBrush,
            RadiusX = 3,
            RadiusY = 3,
            IsHitTestVisible = false,
            Width = Math.Max(1, placement.TextWidth),
            Height = Math.Max(1, bounds.Height / Math.Max(0.001, dpiScaleY))
        };

        Canvas.SetLeft(rectangle, placement.Left);
        Canvas.SetTop(rectangle, Math.Clamp(placement.Top, 0, layout.CanvasHeight));
        WpfPanel.SetZIndex(rectangle, 0);
        CaptionCanvas.Children.Add(rectangle);
        _sourceRegionBackgroundRectangles.Add(rectangle);
    }

    private bool TryPositionCaptionLinesFromSourceLines(
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        OverlayLayout layout,
        double dpiScaleX,
        double dpiScaleY)
    {
        var translatedLines = SplitNonEmptyLines(CaptionText.Text);
        var translatableSourceLines = GetTranslatableBoundedSourceLines(sourceTextLines);
        if (translatedLines.Length == 0 ||
            translatableSourceLines.Length == 0 ||
            translatedLines.Length != translatableSourceLines.Length)
        {
            return false;
        }

        CaptionText.Visibility = Visibility.Collapsed;
        for (var i = 0; i < translatedLines.Length; i++)
        {
            var bounds = translatableSourceLines[i].BoundsPixels!.Value;
            var placement = OverlayLayoutCalculator.CalculateCaptionLinePlacement(
                bounds,
                layout,
                dpiScaleX,
                dpiScaleY,
                minimumTextWidth: 1);
            var lineBlock = CreateCaptionLineTextBlock(translatedLines[i], placement.FontSize);

            lineBlock.Width = Math.Max(1, layout.CanvasWidth - placement.Left);
            Canvas.SetLeft(lineBlock, placement.Left);
            Canvas.SetTop(lineBlock, placement.Top);
            WpfPanel.SetZIndex(lineBlock, 3);
            CaptionCanvas.Children.Add(lineBlock);
            _captionLineTextBlocks.Add(lineBlock);
        }

        return true;
    }

    private OutlinedTextBlock CreateCaptionLineTextBlock(string text, double fontSize)
    {
        return new OutlinedTextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Left,
            FontFamily = CaptionText.FontFamily,
            FontSize = fontSize,
            FontWeight = CaptionText.FontWeight,
            Foreground = CaptionText.Foreground,
            OutlineBrush = CaptionText.OutlineBrush,
            OutlineThickness = CaptionText.OutlineThickness,
            IsOutlineEnabled = _captionOutlineEnabled,
            Effect = _captionOutlineEnabled ? null : CaptionTextShadowEffect,
            IsHitTestVisible = false,
        };
    }

    private void ApplyCaptionTextEffects()
    {
        CaptionText.IsOutlineEnabled = _captionOutlineEnabled;
        CaptionText.Effect = _captionOutlineEnabled ? null : CaptionTextShadowEffect;
        foreach (var lineBlock in _captionLineTextBlocks)
        {
            lineBlock.IsOutlineEnabled = _captionOutlineEnabled;
            lineBlock.Effect = _captionOutlineEnabled ? null : CaptionTextShadowEffect;
        }
    }

    private static DropShadowEffect CreateCaptionTextShadow()
    {
        var effect = new DropShadowEffect
        {
            Color = WpfColor.FromRgb(0, 0, 0),
            BlurRadius = 3,
            ShadowDepth = 0,
            Opacity = 0.9,
            RenderingBias = RenderingBias.Quality
        };
        effect.Freeze();
        return effect;
    }

    private void UpdateCaptionOutlineButtonState()
    {
        CaptionOutlineButton.Content = _captionOutlineEnabled ? "\u63cf" : "A";
        CaptionOutlineButton.Opacity = _captionOutlineEnabled ? 1.0 : 0.55;
        CaptionOutlineButton.ToolTip = _captionOutlineEnabled
            ? Localize("关闭文字黑边", "Disable text outline")
            : Localize("开启文字黑边", "Enable text outline");
    }

    private void UpdateSelectedRegionMarkerButtonState()
    {
        SelectedRegionMarkerButton.Content = "\u6846";
        SelectedRegionMarkerButton.Opacity = _selectedRegionMarkerEnabled ? 1.0 : 0.55;
        SelectedRegionMarkerButton.ToolTip = _selectedRegionMarkerEnabled
            ? Localize("隐藏框选区域", "Hide selected region")
            : Localize("显示框选区域", "Show selected region");
    }

    private void UpdateLocalizedTooltips()
    {
        DragOverlayButton.ToolTip = Localize("按住拖拽 overlay", "Hold to drag overlay");
        BackgroundModeButton.ToolTip = Localize("切换背景透明度", "Toggle overlay background");
        CloseOneShotOverlayButton.ToolTip = Localize("关闭单次 overlay", "Close one-shot overlay");
    }

    private string Localize(string chinese, string english)
    {
        return _uiLanguage == UiLanguage.English ? english : chinese;
    }

    private void PositionSourceTextDebugMarker(
        FrameRegion bounds,
        OverlayLayout layout,
        double dpiScaleX,
        double dpiScaleY,
        string? sourceText)
    {
        if (!ShowDebugMarkers)
        {
            HideSourceTextDebugMarker();
            return;
        }

        var placement = OverlayLayoutCalculator.CalculateTextPlacement(
            bounds,
            layout,
            dpiScaleX,
            dpiScaleY,
            minimumTextWidth: 1);
        var markerHeight = Math.Max(1, bounds.Height / Math.Max(0.001, dpiScaleY));
        var markerTop = Math.Clamp(placement.Top, 0, layout.CanvasHeight);

        SourceTextDebugRectangle.Visibility = Visibility.Visible;
        SourceTextDebugRectangle.Width = Math.Max(1, placement.TextWidth);
        SourceTextDebugRectangle.Height = markerHeight;
        Canvas.SetLeft(SourceTextDebugRectangle, placement.Left);
        Canvas.SetTop(SourceTextDebugRectangle, markerTop);

        SourceTextDebugLabel.Visibility = string.IsNullOrWhiteSpace(sourceText)
            ? Visibility.Collapsed
            : Visibility.Visible;
        SourceTextDebugLabel.Text = sourceText;
        SourceTextDebugLabel.Width = Math.Max(1, Math.Min(placement.TextWidth, layout.CanvasWidth - placement.Left));
        Canvas.SetLeft(SourceTextDebugLabel, placement.Left);
        Canvas.SetTop(SourceTextDebugLabel, Math.Max(0, markerTop - 14));
    }

    private void PositionSourceLineDebugMarkers(
        IReadOnlyList<OcrTextLine>? sourceTextLines,
        OverlayLayout layout,
        double dpiScaleX,
        double dpiScaleY)
    {
        if (!ShowDebugMarkers)
        {
            ClearSourceLineDebugRectangles();
            return;
        }

        foreach (var line in GetTranslatableBoundedSourceLines(sourceTextLines))
        {
            var bounds = line.BoundsPixels!.Value;
            var placement = OverlayLayoutCalculator.CalculateTextPlacement(
                bounds,
                layout,
                dpiScaleX,
                dpiScaleY,
                minimumTextWidth: 1);
            var marker = new WpfRectangle
            {
                Stroke = new SolidColorBrush(WpfColor.FromRgb(0xF0, 0x8A, 0x24)),
                StrokeThickness = 1.5,
                Fill = new SolidColorBrush(WpfColor.FromArgb(0x18, 0xF0, 0x8A, 0x24)),
                IsHitTestVisible = false,
                Width = Math.Max(1, placement.TextWidth),
                Height = Math.Max(1, bounds.Height / Math.Max(0.001, dpiScaleY))
            };

            Canvas.SetLeft(marker, placement.Left);
            Canvas.SetTop(marker, Math.Clamp(placement.Top, 0, layout.CanvasHeight));
            WpfPanel.SetZIndex(marker, 1);
            CaptionCanvas.Children.Add(marker);
            _sourceLineDebugRectangles.Add(marker);
        }
    }

    private void HideDebugMarkers()
    {
        HideSourceTextDebugMarker();
    }

    private void HideSourceTextDebugMarker()
    {
        SourceTextDebugRectangle.Visibility = Visibility.Collapsed;
        SourceTextDebugLabel.Visibility = Visibility.Collapsed;
        SourceTextDebugLabel.Text = string.Empty;
        ClearSourceLineDebugRectangles();
    }

    private void PositionCaptionFallback()
    {
        ClearCaptionLineTextBlocks();
        CaptionText.Visibility = Visibility.Visible;
        CaptionText.Width = Math.Max(MinimumTextWidth, CaptionCanvas.ActualWidth > 0 ? CaptionCanvas.ActualWidth : Width);
        CaptionText.TextAlignment = TextAlignment.Left;
        Canvas.SetLeft(CaptionText, 0);
        Canvas.SetTop(CaptionText, 0);
    }

    private void ClearCaptionLineTextBlocks()
    {
        foreach (var lineBlock in _captionLineTextBlocks)
        {
            CaptionCanvas.Children.Remove(lineBlock);
        }

        _captionLineTextBlocks.Clear();
    }

    private void ClearSourceRegionBackgroundRectangles()
    {
        foreach (var rectangle in _sourceRegionBackgroundRectangles)
        {
            CaptionCanvas.Children.Remove(rectangle);
        }

        _sourceRegionBackgroundRectangles.Clear();
    }

    private void ClearSourceLineDebugRectangles()
    {
        foreach (var marker in _sourceLineDebugRectangles)
        {
            CaptionCanvas.Children.Remove(marker);
        }

        _sourceLineDebugRectangles.Clear();
    }

    private static string[] SplitNonEmptyLines(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();
    }

    private OcrTextLine[] GetTranslatableBoundedSourceLines(IReadOnlyList<OcrTextLine>? sourceTextLines)
    {
        return OcrLineTextExtractor.GetTranslatableBoundedLines(sourceTextLines, _targetLanguage);
    }

    private void ApplyCaptionFontSize(double? sourceTextHeightPixels)
    {
        if (sourceTextHeightPixels is not > 0)
        {
            CaptionText.FontSize = OverlayLayoutCalculator.DefaultCaptionFontSize;
            ProviderLabelText.FontSize = 10;
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var fontSize = OverlayLayoutCalculator.CalculateCaptionFontSize(
            sourceTextHeightPixels,
            dpi.DpiScaleY);
        CaptionText.FontSize = fontSize;
        ProviderLabelText.FontSize = Math.Clamp(fontSize * 0.38, 9, 14);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
