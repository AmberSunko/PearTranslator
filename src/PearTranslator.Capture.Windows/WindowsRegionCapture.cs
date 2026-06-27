using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using PearTranslator.Core.Abstractions;

namespace PearTranslator.Capture.Windows;

public sealed class WindowsRegionCapture : IRegionCapture
{
    private readonly object _regionLock = new();
    private FrameRegion? _region;

    public bool HasRegion
    {
        get
        {
            lock (_regionLock)
            {
                return _region.HasValue;
            }
        }
    }

    public FrameRegion? CurrentRegion
    {
        get
        {
            lock (_regionLock)
            {
                return _region;
            }
        }
    }

    public void SetRegion(FrameRegion region)
    {
        ValidateRegion(region);

        lock (_regionLock)
        {
            _region = region;
        }
    }

    public Task<CapturedFrame> CaptureAsync(CancellationToken cancellationToken)
    {
        FrameRegion region;
        lock (_regionLock)
        {
            region = _region ?? throw new InvalidOperationException("A subtitle region must be selected before capture starts.");
        }

        return Capture(region, cancellationToken);
    }

    public Task<CapturedFrame> CaptureRegionAsync(FrameRegion region, CancellationToken cancellationToken)
    {
        ValidateRegion(region);
        return Capture(region, cancellationToken);
    }

    private static void ValidateRegion(FrameRegion region)
    {
        if (region.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(region), "Capture region width must be positive.");
        }

        if (region.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(region), "Capture region height must be positive.");
        }
    }

    private static async Task<CapturedFrame> Capture(FrameRegion region, CancellationToken cancellationToken)
    {
        try
        {
            return await WindowsGraphicsCaptureBackend.CaptureAsync(region, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return await Task.Run(() => CaptureWithGdi(region, cancellationToken), cancellationToken);
        }
    }

    private static CapturedFrame CaptureWithGdi(FrameRegion region, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                region.X,
                region.Y,
                0,
                0,
                new Size(region.Width, region.Height),
                CopyPixelOperation.SourceCopy);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var imageBytes = CopyBgra32Bytes(bitmap);

        return new CapturedFrame(
            region,
            DateTimeOffset.UtcNow,
            SHA256.HashData(imageBytes),
            imageBytes,
            CapturedFrame.RawBgra32MimeType,
            region.Width,
            region.Height);
    }

    private static byte[] CopyBgra32Bytes(Bitmap bitmap)
    {
        var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            var rowBytes = bitmap.Width * 4;
            var pixels = new byte[rowBytes * bitmap.Height];
            for (var row = 0; row < bitmap.Height; row++)
            {
                var sourceRow = data.Stride < 0 ? bitmap.Height - 1 - row : row;
                var source = IntPtr.Add(data.Scan0, sourceRow * data.Stride);
                Marshal.Copy(source, pixels, row * rowBytes, rowBytes);
            }

            return pixels;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
