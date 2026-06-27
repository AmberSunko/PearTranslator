using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace PearTranslator.App.Wpf.Tests;

public sealed class ApplicationIconTests
{
    [Fact]
    public void ProjectUsesPearIconForExecutable()
    {
        var project = XDocument.Load(GetAppProjectPath());

        var applicationIcon = project.Descendants("ApplicationIcon").SingleOrDefault();
        Assert.NotNull(applicationIcon);
        Assert.Equal(@"Assets\pear.ico", applicationIcon.Value);

        Assert.Contains(project.Descendants("Resource"), resource =>
            (string?)resource.Attribute("Include") == @"Assets\pear.ico");
    }

    [Fact]
    public void MainWindowUsesPearIconForTaskbar()
    {
        var mainWindow = XDocument.Load(GetMainWindowXamlPath());

        Assert.Equal("Assets/pear.ico", (string?)mainWindow.Root?.Attribute("Icon"));
    }

    [Fact]
    public void TrayIconUsesPearIconResource()
    {
        var source = File.ReadAllText(GetTrayIconServicePath());

        Assert.Contains("_trayIcon = LoadTrayIcon()", source);
        Assert.Contains("Icon = _trayIcon", source);
        Assert.Contains("pack://application:,,,/Assets/pear.ico", source);
        Assert.DoesNotContain("Icon = System.Drawing.SystemIcons.Application", source);
    }

    [Fact]
    public void PearIconAssetExists()
    {
        Assert.True(File.Exists(GetPearIconPath()));
    }

    [Fact]
    public void PearIconKeepsSmallLauncherFrameCompleteAndBalanced()
    {
        var frame = DecodeIconFrame(GetPearIconPath(), 32);
        var bounds = GetVisibleBounds(frame);

        Assert.True(bounds.X > 0, $"Expected left margin, actual bounds {bounds}.");
        Assert.True(bounds.Y > 0, $"Expected top margin, actual bounds {bounds}.");
        Assert.True(bounds.X + bounds.Width < frame.PixelWidth, $"Expected right margin, actual bounds {bounds}.");
        Assert.True(bounds.Y + bounds.Height < frame.PixelHeight, $"Expected bottom margin, actual bounds {bounds}.");
        Assert.InRange(bounds.Width, 22, 25);
        Assert.InRange(bounds.Height, 29, 31);
    }

    private static string GetAppProjectPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "PearTranslator.App.Wpf",
            "PearTranslator.App.Wpf.csproj"));
    }

    private static string GetMainWindowXamlPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "PearTranslator.App.Wpf",
            "MainWindow.xaml"));
    }

    private static string GetTrayIconServicePath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "PearTranslator.App.Wpf",
            "Tray",
            "TrayIconService.cs"));
    }

    private static string GetPearIconPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "PearTranslator.App.Wpf",
            "Assets",
            "pear.ico"));
    }

    private static BitmapSource DecodeIconFrame(string path, int size)
    {
        var bytes = File.ReadAllBytes(path);
        var count = BitConverter.ToUInt16(bytes, 4);
        for (var index = 0; index < count; index++)
        {
            var entryOffset = 6 + index * 16;
            var width = bytes[entryOffset] == 0 ? 256 : bytes[entryOffset];
            var height = bytes[entryOffset + 1] == 0 ? 256 : bytes[entryOffset + 1];
            if (width != size || height != size)
            {
                continue;
            }

            var byteCount = BitConverter.ToInt32(bytes, entryOffset + 8);
            var imageOffset = BitConverter.ToInt32(bytes, entryOffset + 12);
            using var stream = new MemoryStream(bytes, imageOffset, byteCount, writable: false);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Pbgra32, null, 0);
        }

        throw new InvalidOperationException($"Icon frame {size}px was not found.");
    }

    private static Int32Rect GetVisibleBounds(BitmapSource bitmap)
    {
        var stride = bitmap.PixelWidth * 4;
        var pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);

        var minX = bitmap.PixelWidth;
        var minY = bitmap.PixelHeight;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < bitmap.PixelHeight; y++)
        {
            for (var x = 0; x < bitmap.PixelWidth; x++)
            {
                var alpha = pixels[y * stride + x * 4 + 3];
                if (alpha <= 10)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return maxX < minX
            ? Int32Rect.Empty
            : new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
}
