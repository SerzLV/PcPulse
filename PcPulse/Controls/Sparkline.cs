using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;

namespace PcPulse.Controls;

/// <summary>
/// Rolling line chart, newest value on the right. Redraws when <see cref="Values"/> raises CollectionChanged.
/// </summary>
public sealed class Sparkline : FrameworkElement
{
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IReadOnlyList<double>), typeof(Sparkline),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnValuesChanged));

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color), typeof(Color), typeof(Sparkline),
        new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MinScaleProperty = DependencyProperty.Register(
        nameof(MinScale), typeof(double), typeof(Sparkline),
        new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Pen GridPen = CreateGridPen();

    public IReadOnlyList<double>? Values
    {
        get => (IReadOnlyList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>The Y axis never shrinks below this, so a flat idle line stays low.</summary>
    public double MinScale
    {
        get => (double)GetValue(MinScaleProperty);
        set => SetValue(MinScaleProperty, value);
    }

    /// <summary>How many points fit across the full width.</summary>
    public int Capacity { get; set; } = 60;

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (Sparkline)d;
        if (e.OldValue is INotifyCollectionChanged oldValues) oldValues.CollectionChanged -= chart.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newValues) newValues.CollectionChanged += chart.OnCollectionChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        for (int i = 1; i <= 2; i++)
            dc.DrawLine(GridPen, new Point(0, h * i / 3), new Point(w, h * i / 3));

        var values = Values;
        if (values is null || values.Count < 2) return;

        double max = Math.Max(MinScale, values.Max() * 1.15);
        double step = w / (Capacity - 1);
        double x0 = w - step * (values.Count - 1);
        var points = values.Select((v, i) => new Point(x0 + i * step, h - Math.Clamp(v / max, 0, 1) * (h - 2) - 1)).ToList();

        var line = new StreamGeometry();
        using (var ctx = line.Open())
        {
            ctx.BeginFigure(points[0], false, false);
            ctx.PolyLineTo(points.Skip(1).ToList(), true, true);
        }
        var area = new StreamGeometry();
        using (var ctx = area.Open())
        {
            ctx.BeginFigure(new Point(points[0].X, h), true, true);
            ctx.PolyLineTo(points.Append(new Point(points[^1].X, h)).ToList(), false, false);
        }

        var c = Color;
        var fill = new LinearGradientBrush(Color.FromArgb(0x55, c.R, c.G, c.B), Color.FromArgb(0x00, c.R, c.G, c.B), 90);
        dc.DrawGeometry(fill, null, area);
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(c), 2) { LineJoin = PenLineJoin.Round }, line);
        dc.DrawEllipse(new SolidColorBrush(c), null, points[^1], 3.5, 3.5);
    }

    private static Pen CreateGridPen()
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(0x30, 0x7C, 0x86, 0x9A)), 1) { DashStyle = DashStyles.Dot };
        pen.Freeze();
        return pen;
    }
}
