using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace PearTranslator.App.Wpf;

internal static class WindowBackdropService
{
    private const int DwmWindowCornerPreference = 33;
    private const int DwmBorderColor = 34;
    private const int DwmSystemBackdropType = 38;
    private const int DwmColorNone = unchecked((int)0xFFFFFFFE);

    public static void ApplyLiquidGlass(
        Window window,
        double topGlassHeightDip,
        double bottomGlassHeightDip,
        bool isActive)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        if (HwndSource.FromHwnd(hwnd) is { CompositionTarget: not null } source)
        {
            source.CompositionTarget.BackgroundColor = Colors.Transparent;
        }

        var dpi = VisualTreeHelper.GetDpi(window);
        var frame = DwmGlassFrame.FromDeviceIndependentPixels(
            topGlassHeightDip,
            bottomGlassHeightDip,
            dpi.DpiScaleY);
        var margins = new DwmMargins
        {
            LeftWidth = 0,
            RightWidth = 0,
            TopHeight = frame.TopPixels,
            BottomHeight = frame.BottomPixels
        };
        _ = DwmExtendFrameIntoClientArea(hwnd, ref margins);

        SetDwmAttribute(hwnd, DwmWindowCornerPreference, (int)WindowCornerPreference.Round);
        SetDwmAttribute(hwnd, DwmBorderColor, DwmColorNone);

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
        {
            SetDwmAttribute(
                hwnd,
                DwmSystemBackdropType,
                isActive ? (int)SystemBackdropType.TransientWindow : (int)SystemBackdropType.None);
        }
    }

    private static void SetDwmAttribute(IntPtr hwnd, int attribute, int value)
    {
        _ = DwmSetWindowAttribute(hwnd, attribute, ref value, Marshal.SizeOf<int>());
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmExtendFrameIntoClientArea(
        IntPtr hwnd,
        ref DwmMargins margins);

    [StructLayout(LayoutKind.Sequential)]
    private struct DwmMargins
    {
        public int LeftWidth;
        public int RightWidth;
        public int TopHeight;
        public int BottomHeight;
    }

    private enum WindowCornerPreference
    {
        Round = 2
    }

    private enum SystemBackdropType
    {
        None = 1,
        TransientWindow = 3
    }
}
