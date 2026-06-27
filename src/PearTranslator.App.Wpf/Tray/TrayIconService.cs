using System.Windows;
using DrawingIcon = System.Drawing.Icon;
using Forms = System.Windows.Forms;

namespace PearTranslator.App.Wpf.Tray;

public sealed class TrayIconService : IDisposable
{
    private readonly Window _mainWindow;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly DrawingIcon _trayIcon;

    public TrayIconService(Window mainWindow)
    {
        _mainWindow = mainWindow;
        _trayIcon = LoadTrayIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "PearTranslator",
            Icon = _trayIcon,
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _trayIcon.Dispose();
    }

    private Forms.ContextMenuStrip BuildMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("打开", null, (_, _) => ShowMainWindow());
        menu.Items.Add("退出", null, (_, _) => System.Windows.Application.Current.Shutdown());
        return menu;
    }

    private void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private static DrawingIcon LoadTrayIcon()
    {
        var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/pear.ico", UriKind.Absolute));
        if (resource is null)
        {
            throw new InvalidOperationException("Pear tray icon resource was not found.");
        }

        using var stream = resource.Stream;
        using var icon = new DrawingIcon(stream);
        return (DrawingIcon)icon.Clone();
    }
}
