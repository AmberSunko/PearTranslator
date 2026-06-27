using System.Runtime.InteropServices;
using System.Security.Cryptography;
using PearTranslator.Core.Abstractions;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WinRT;
using static Vortice.Direct3D11.D3D11;

namespace PearTranslator.Capture.Windows;

internal static class WindowsGraphicsCaptureBackend
{
    private static readonly Guid GraphicsCaptureItemGuid = typeof(GraphicsCaptureItem).GUID;
    private static readonly Lazy<IDirect3DDevice> SharedDirect3DDevice = new(
        CreateDirect3DDevice,
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly SemaphoreSlim CaptureGate = new(1, 1);
    private static CaptureSessionState? _activeSession;

    public static async Task<CapturedFrame> CaptureAsync(FrameRegion region, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await CaptureGate.WaitAsync(cancellationToken);

        try
        {
            var monitorHandle = MonitorFromRegion(region);
            var session = GetOrCreateSession(monitorHandle);
            using var frame = await ReadLatestFrameAsync(session.FramePool, cancellationToken);
            using var softwareBitmap = await SoftwareBitmap
                .CreateCopyFromSurfaceAsync(frame.Surface, BitmapAlphaMode.Premultiplied)
                .AsTask(cancellationToken);
            var image = CopyRegionBgra32(softwareBitmap, region, session.MonitorBounds);

            return new CapturedFrame(
                region,
                DateTimeOffset.UtcNow,
                SHA256.HashData(image.Bytes),
                image.Bytes,
                CapturedFrame.RawBgra32MimeType,
                image.Width,
                image.Height);
        }
        finally
        {
            CaptureGate.Release();
        }
    }

    private static CaptureSessionState GetOrCreateSession(IntPtr monitorHandle)
    {
        var monitorBounds = GetMonitorBounds(monitorHandle);
        if (_activeSession is { } active &&
            active.MonitorHandle == monitorHandle &&
            active.MonitorBounds.Equals(monitorBounds))
        {
            return active;
        }

        _activeSession?.Dispose();
        var item = CreateItemForMonitor(monitorHandle);
        var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            SharedDirect3DDevice.Value,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            item.Size);
        var session = framePool.CreateCaptureSession(item);
        session.IsCursorCaptureEnabled = false;
        session.StartCapture();

        _activeSession = new CaptureSessionState(monitorHandle, monitorBounds, item, framePool, session);
        return _activeSession;
    }

    private static async Task<Direct3D11CaptureFrame> ReadLatestFrameAsync(
        Direct3D11CaptureFramePool framePool,
        CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(750));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        while (true)
        {
            linked.Token.ThrowIfCancellationRequested();
            Direct3D11CaptureFrame? latest = null;
            while (framePool.TryGetNextFrame() is { } frame)
            {
                latest?.Dispose();
                latest = frame;
            }

            if (latest is not null)
            {
                return latest;
            }

            await Task.Delay(8, linked.Token);
        }
    }

    private static RawBgra32Image CopyRegionBgra32(
        SoftwareBitmap softwareBitmap,
        FrameRegion region,
        RECT monitorBounds)
    {
        SoftwareBitmap? converted = null;
        var bitmap = softwareBitmap;
        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
            softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
        {
            converted = SoftwareBitmap.Convert(
                softwareBitmap,
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);
            bitmap = converted;
        }

        try
        {
            var left = Math.Clamp(region.X - monitorBounds.Left, 0, Math.Max(0, bitmap.PixelWidth - 1));
            var top = Math.Clamp(region.Y - monitorBounds.Top, 0, Math.Max(0, bitmap.PixelHeight - 1));
            var width = Math.Clamp(region.Width, 1, bitmap.PixelWidth - left);
            var height = Math.Clamp(region.Height, 1, bitmap.PixelHeight - top);

            var fullBytes = CopyBitmapBytes(bitmap);
            var rowBytes = width * 4;
            var croppedBytes = new byte[rowBytes * height];
            var sourceStride = bitmap.PixelWidth * 4;

            for (var row = 0; row < height; row++)
            {
                System.Buffer.BlockCopy(
                    fullBytes,
                    ((top + row) * sourceStride) + (left * 4),
                    croppedBytes,
                    row * rowBytes,
                    rowBytes);
            }

            return new RawBgra32Image(croppedBytes, width, height);
        }
        finally
        {
            converted?.Dispose();
        }
    }

    private static byte[] CopyBitmapBytes(SoftwareBitmap bitmap)
    {
        var byteCount = bitmap.PixelWidth * bitmap.PixelHeight * 4;
        var buffer = new global::Windows.Storage.Streams.Buffer((uint)byteCount);
        bitmap.CopyToBuffer(buffer);

        var bytes = new byte[byteCount];
        using var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(bytes);
        return bytes;
    }

    private static IDirect3DDevice CreateDirect3DDevice()
    {
        D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            null,
            out ID3D11Device? d3dDevice).CheckError();

        if (d3dDevice is null)
        {
            throw new InvalidOperationException("Unable to create a D3D11 device for capture.");
        }

        using var dxgiDevice = d3dDevice.QueryInterface<IDXGIDevice>();
        CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var inspectable).ThrowOnFailure();
        return MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
    }

    private static GraphicsCaptureItem CreateItemForMonitor(IntPtr monitorHandle)
    {
        var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
        var iid = GraphicsCaptureItemGuid;
        var itemPointer = interop.CreateForMonitor(monitorHandle, ref iid);
        return MarshalInterface<GraphicsCaptureItem>.FromAbi(itemPointer);
    }

    private static IntPtr MonitorFromRegion(FrameRegion region)
    {
        var rect = new RECT
        {
            Left = region.X,
            Top = region.Y,
            Right = region.X + region.Width,
            Bottom = region.Y + region.Height
        };

        var monitor = MonitorFromRect(ref rect, MonitorDefaultToNearest);
        return monitor == IntPtr.Zero
            ? throw new InvalidOperationException("Unable to find a monitor for the capture region.")
            : monitor;
    }

    private static RECT GetMonitorBounds(IntPtr monitorHandle)
    {
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitorHandle, ref info))
        {
            throw new InvalidOperationException("Unable to read monitor bounds for capture.");
        }

        return info.rcMonitor;
    }

    [DllImport("d3d11.dll")]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(
        IntPtr dxgiDevice,
        out IntPtr graphicsDevice);

    private static void ThrowOnFailure(this int hresult)
    {
        if (hresult < 0)
        {
            Marshal.ThrowExceptionForHR(hresult);
        }
    }

    private const uint MonitorDefaultToNearest = 0x00000002;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref RECT lprc, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(IntPtr windowHandle, ref Guid iid);

        IntPtr CreateForMonitor(IntPtr monitorHandle, ref Guid iid);
    }

    private sealed record RawBgra32Image(byte[] Bytes, int Width, int Height);

    private sealed class CaptureSessionState : IDisposable
    {
        public CaptureSessionState(
            IntPtr monitorHandle,
            RECT monitorBounds,
            GraphicsCaptureItem item,
            Direct3D11CaptureFramePool framePool,
            GraphicsCaptureSession session)
        {
            MonitorHandle = monitorHandle;
            MonitorBounds = monitorBounds;
            Item = item;
            FramePool = framePool;
            Session = session;
        }

        public IntPtr MonitorHandle { get; }

        public RECT MonitorBounds { get; }

        public GraphicsCaptureItem Item { get; }

        public Direct3D11CaptureFramePool FramePool { get; }

        public GraphicsCaptureSession Session { get; }

        public void Dispose()
        {
            Session.Dispose();
            FramePool.Dispose();
        }
    }
}
