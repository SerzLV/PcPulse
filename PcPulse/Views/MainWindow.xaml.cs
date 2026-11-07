using System.Windows;
using System.Windows.Interop;
using PcPulse.Platform;
using PcPulse.Shell;
using PcPulse.ViewModels;

namespace PcPulse.Views;

/// <summary>Passive view: reports its visibility to the view model and window events to the shell.</summary>
public partial class MainWindow : Window, IMainWindowView
{
    private readonly MainViewModel _model;

    public MainWindow(MainViewModel model)
    {
        InitializeComponent();
        DataContext = _model = model;
        StateChanged += OnStateChanged;
        IsVisibleChanged += (_, _) => UpdateVisibility();
    }

    public event EventHandler? Minimized;

    public bool IsInFront =>
        IsVisible && WindowState != WindowState.Minimized
        // WPF's IsActive can stay true while another app is in front (e.g. right after launch),
        // so ask Windows which window really has the foreground.
        && NativeMethods.IsForeground(new WindowInteropHelper(this).Handle);

    public void ShowAndActivate()
    {
        // Restore before showing so a window hidden while minimized doesn't flash minimized first.
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Show();
        Activate();
    }

    public void Minimize() => WindowState = WindowState.Minimized;

    private void OnStateChanged(object? sender, EventArgs e)
    {
        Root.Margin = WindowState == WindowState.Maximized
            ? new Thickness(24, 8, 24, 20)
            : new Thickness(16, 0, 16, 14);
        UpdateVisibility();
        if (WindowState == WindowState.Minimized) Minimized?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateVisibility() => _model.IsViewVisible = IsVisible && WindowState != WindowState.Minimized;
}
