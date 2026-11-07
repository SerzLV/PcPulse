using System.Windows;
using System.Windows.Interop;
using PcPulse.Core.Settings;
using PcPulse.Platform;
using PcPulse.Shell;
using PcPulse.ViewModels;

namespace PcPulse.Views;

/// <summary>Small click-through, always-on-top panel for use over games (borderless/windowed modes).</summary>
public partial class OverlayWindow : Window, IOverlayView
{
    private const double ScreenMargin = 16;

    private IntPtr _hwnd;
    private OverlayCorner _corner;

    public OverlayWindow(OverlayViewModel model)
    {
        InitializeComponent();
        DataContext = model;
        model.Updated += KeepOnTop;
        Closed += (_, _) => model.Updated -= KeepOnTop;
        // Size changes with the numbers' width, so keep re-anchoring to the chosen corner.
        SizeChanged += (_, _) => Reposition();
    }

    public void SetCorner(OverlayCorner corner)
    {
        _corner = corner;
        Reposition();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.MakeClickThrough(_hwnd);
    }

    private void KeepOnTop()
    {
        if (_hwnd != IntPtr.Zero && IsVisible) NativeMethods.BringToTop(_hwnd);
    }

    private void Reposition()
    {
        var area = SystemParameters.WorkArea;
        double w = ActualWidth > 0 ? ActualWidth : Width;
        double h = ActualHeight > 0 ? ActualHeight : Height;
        if (double.IsNaN(w) || double.IsNaN(h)) w = h = 0;

        bool right = _corner is OverlayCorner.TopRight or OverlayCorner.BottomRight;
        bool bottom = _corner is OverlayCorner.BottomLeft or OverlayCorner.BottomRight;
        Left = right ? area.Right - w - ScreenMargin : area.Left + ScreenMargin;
        Top = bottom ? area.Bottom - h - ScreenMargin : area.Top + ScreenMargin;
    }
}
