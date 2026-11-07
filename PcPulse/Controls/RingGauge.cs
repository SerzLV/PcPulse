using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace PcPulse.Controls;

/// <summary>A glowing 270° arc gauge. Bind <see cref="Target"/>; the arc animates towards it.</summary>
public sealed class RingGauge : FrameworkElement
{
    private static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(RingGauge),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
        nameof(Target), typeof(double), typeof(RingGauge),
        new PropertyMetadata(0.0, (d, e) => ((RingGauge)d).AnimateTo((double)e.NewValue)));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(RingGauge),
        new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color), typeof(Color), typeof(RingGauge),
        new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.AffectsRender,
            (d, e) => ((RingGauge)d)._glow.Color = (Color)e.NewValue));

    private static readonly Brush TrackBrush = Frozen(new SolidColorBrush(Color.FromRgb(0x1E, 0x26, 0x36)));

    private readonly DropShadowEffect _glow = new() { BlurRadius = 22, ShadowDepth = 0, Opacity = 0.55 };

    public RingGauge() => Effect = _glow;

    /// <summary>The value currently drawn (mid-animation).</summary>
    private double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Target
    {
        get => (double)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public double Thickness { get; set; } = 12;

    private void AnimateTo(double value) =>
        BeginAnimation(ValueProperty, new DoubleAnimation(value, TimeSpan.FromMilliseconds(600))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        });

    protected override void OnRender(DrawingContext dc)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size <= Thickness) return;

        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        double r = (size - Thickness) / 2;
        const double start = 135, sweep = 270;

        DrawArc(dc, center, r, start, sweep, MakePen(TrackBrush));
        double fraction = Maximum > 0 ? Math.Clamp(Value / Maximum, 0, 1) : 0;
        if (fraction > 0.002)
            DrawArc(dc, center, r, start, sweep * fraction, MakePen(new SolidColorBrush(Color)));
    }

    private Pen MakePen(Brush brush) => new(brush, Thickness)
    {
        StartLineCap = PenLineCap.Round,
        EndLineCap = PenLineCap.Round,
    };

    private static void DrawArc(DrawingContext dc, Point c, double r, double startDeg, double sweepDeg, Pen pen)
    {
        Point At(double deg) => new(c.X + r * Math.Cos(deg * Math.PI / 180), c.Y + r * Math.Sin(deg * Math.PI / 180));

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(At(startDeg), false, false);
            ctx.ArcTo(At(startDeg + sweepDeg), new Size(r, r), 0, sweepDeg > 180, SweepDirection.Clockwise, true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }

    private static T Frozen<T>(T f) where T : Freezable
    {
        f.Freeze();
        return f;
    }
}
