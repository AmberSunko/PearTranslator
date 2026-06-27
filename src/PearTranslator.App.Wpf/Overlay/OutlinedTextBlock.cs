using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfPen = System.Windows.Media.Pen;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;
using WpfSystemFonts = System.Windows.SystemFonts;

namespace PearTranslator.App.Wpf.Overlay;

public sealed class OutlinedTextBlock : FrameworkElement
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
        nameof(TextWrapping),
        typeof(TextWrapping),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(TextWrapping.NoWrap, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.Register(
        nameof(TextTrimming),
        typeof(TextTrimming),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(TextTrimming.None, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
        nameof(TextAlignment),
        typeof(TextAlignment),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(TextAlignment.Left, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
        nameof(FontFamily),
        typeof(WpfFontFamily),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(WpfSystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
        nameof(FontSize),
        typeof(double),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(WpfSystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
        nameof(FontWeight),
        typeof(FontWeight),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
        nameof(Foreground),
        typeof(WpfBrush),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(WpfBrushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OutlineBrushProperty = DependencyProperty.Register(
        nameof(OutlineBrush),
        typeof(WpfBrush),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(WpfBrushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OutlineThicknessProperty = DependencyProperty.Register(
        nameof(OutlineThickness),
        typeof(double),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsOutlineEnabledProperty = DependencyProperty.Register(
        nameof(IsOutlineEnabled),
        typeof(bool),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public TextTrimming TextTrimming
    {
        get => (TextTrimming)GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => (TextAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public WpfFontFamily FontFamily
    {
        get => (WpfFontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public WpfBrush Foreground
    {
        get => (WpfBrush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public WpfBrush OutlineBrush
    {
        get => (WpfBrush)GetValue(OutlineBrushProperty);
        set => SetValue(OutlineBrushProperty, value);
    }

    public double OutlineThickness
    {
        get => (double)GetValue(OutlineThicknessProperty);
        set => SetValue(OutlineThicknessProperty, value);
    }

    public bool IsOutlineEnabled
    {
        get => (bool)GetValue(IsOutlineEnabledProperty);
        set => SetValue(IsOutlineEnabledProperty, value);
    }

    protected override WpfSize MeasureOverride(WpfSize availableSize)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return new WpfSize(0, FontSize);
        }

        var formattedText = CreateFormattedText(ReadMeasureWidth(availableSize.Width));
        return new WpfSize(
            double.IsInfinity(availableSize.Width)
                ? formattedText.WidthIncludingTrailingWhitespace
                : Math.Min(availableSize.Width, formattedText.WidthIncludingTrailingWhitespace),
            formattedText.Height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        var formattedText = CreateFormattedText(Math.Max(1, ActualWidth));
        var geometry = formattedText.BuildGeometry(new WpfPoint(0, 0));
        if (IsOutlineEnabled && OutlineThickness > 0)
        {
            var pen = new WpfPen(OutlineBrush, OutlineThickness * 2)
            {
                LineJoin = PenLineJoin.Round
            };
            drawingContext.DrawGeometry(null, pen, geometry);
        }

        drawingContext.DrawGeometry(Foreground, null, geometry);
    }

    private FormattedText CreateFormattedText(double maxWidth)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var formattedText = new FormattedText(
            Text,
            CultureInfo.CurrentUICulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal),
            FontSize,
            Foreground,
            dpi.PixelsPerDip)
        {
            MaxTextWidth = Math.Max(1, maxWidth),
            TextAlignment = TextAlignment,
            Trimming = TextTrimming
        };

        if (TextWrapping == TextWrapping.NoWrap)
        {
            formattedText.MaxLineCount = 1;
        }

        return formattedText;
    }

    private static double ReadMeasureWidth(double availableWidth)
    {
        return double.IsInfinity(availableWidth) || double.IsNaN(availableWidth)
            ? 4096
            : Math.Max(1, availableWidth);
    }
}
