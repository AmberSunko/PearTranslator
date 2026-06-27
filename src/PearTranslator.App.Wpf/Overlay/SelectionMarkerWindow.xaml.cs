using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using PearTranslator.Core.Abstractions;

namespace PearTranslator.App.Wpf.Overlay;

public partial class SelectionMarkerWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolwindow = 0x00000080;
    private const int WsExNoactivate = 0x08000000;
    private const uint WdaNone = 0x00000000;
    private const uint WdaExcludeFromCapture = 0x00000011;
    private const uint WdaMonitor = 0x00000001;

    private readonly WindowDisplayAffinityState _displayAffinityState = new();
    private bool _excludeFromCaptureEnabled = true;

    public SelectionMarkerWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ConfigureWindowInterop();
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

    public void ShowRegion(FrameRegion region)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        Left = region.X / dpi.DpiScaleX;
        Top = region.Y / dpi.DpiScaleY;
        Width = Math.Max(1, region.Width / dpi.DpiScaleX);
        Height = Math.Max(1, region.Height / dpi.DpiScaleY);

        if (!IsVisible)
        {
            Show();
        }

        ApplyCaptureAffinityForCurrentState();
    }

    private void ConfigureWindowInterop()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var currentStyle = GetWindowLong(handle, GwlExstyle);
        SetWindowLong(handle, GwlExstyle, currentStyle | WsExToolwindow | WsExNoactivate | WsExTransparent);
        ApplyCaptureAffinityForCurrentState(handle);
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

        if (!_displayAffinityState.TryMarkRequested(requestedMode))
        {
            return;
        }

        if (requestedMode == WindowDisplayAffinityMode.ExcludedFromCapture)
        {
            if (!SetWindowDisplayAffinity(handle, WdaExcludeFromCapture))
            {
                SetWindowDisplayAffinity(handle, WdaMonitor);
            }

            return;
        }

        SetWindowDisplayAffinity(handle, WdaNone);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
}
