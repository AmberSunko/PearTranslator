using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PearTranslator.Core.Abstractions;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace PearTranslator.App.Wpf.RegionSelection;

public partial class RegionSelectionWindow : Window
{
    private WpfPoint? _start;

    public RegionSelectionWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Focus();
    }

    public FrameRegion? SelectedRegion { get; private set; }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        SelectionCanvas.ReleaseMouseCapture();
        DialogResult = false;
        e.Handled = true;
        Close();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(SelectionCanvas);
        SelectionRectangle.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRectangle, _start.Value.X);
        Canvas.SetTop(SelectionRectangle, _start.Value.Y);
        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;
        SelectionCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_start is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(SelectionCanvas);
        var left = Math.Min(_start.Value.X, current.X);
        var top = Math.Min(_start.Value.Y, current.Y);
        var width = Math.Abs(current.X - _start.Value.X);
        var height = Math.Abs(current.Y - _start.Value.Y);

        Canvas.SetLeft(SelectionRectangle, left);
        Canvas.SetTop(SelectionRectangle, top);
        SelectionRectangle.Width = width;
        SelectionRectangle.Height = height;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_start is null)
        {
            return;
        }

        SelectionCanvas.ReleaseMouseCapture();
        var current = e.GetPosition(SelectionCanvas);
        var left = Math.Min(_start.Value.X, current.X);
        var top = Math.Min(_start.Value.Y, current.Y);
        var width = Math.Abs(current.X - _start.Value.X);
        var height = Math.Abs(current.Y - _start.Value.Y);

        if (width >= 30 && height >= 20)
        {
            SelectedRegion = ToPhysicalScreenRegion(left, top, width, height);
            DialogResult = true;
        }
        else
        {
            DialogResult = false;
        }

        Close();
    }

    private FrameRegion ToPhysicalScreenRegion(double left, double top, double width, double height)
    {
        var topLeft = PointToScreen(new WpfPoint(left, top));
        var bottomRight = PointToScreen(new WpfPoint(left + width, top + height));

        var screenLeft = (int)Math.Round(Math.Min(topLeft.X, bottomRight.X));
        var screenTop = (int)Math.Round(Math.Min(topLeft.Y, bottomRight.Y));
        var screenWidth = (int)Math.Round(Math.Abs(bottomRight.X - topLeft.X));
        var screenHeight = (int)Math.Round(Math.Abs(bottomRight.Y - topLeft.Y));

        return new FrameRegion(screenLeft, screenTop, screenWidth, screenHeight);
    }
}
