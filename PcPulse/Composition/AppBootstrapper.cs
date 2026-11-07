using System.Windows;
using PcPulse.Core.Fps;
using PcPulse.Core.Monitoring;
using PcPulse.Infrastructure;
using PcPulse.Infrastructure.Diagnostics;
using PcPulse.Infrastructure.Fps;
using PcPulse.Infrastructure.Hardware;
using PcPulse.Infrastructure.Storage;
using PcPulse.Infrastructure.Windows;
using PcPulse.Platform;
using PcPulse.Shell;
using PcPulse.ViewModels;
using PcPulse.ViewModels.Cards;
using PcPulse.Views;

namespace PcPulse.Composition;

/// <summary>
/// Composition root: decides whether this launch should run at all (dump mode, second copy, elevation)
/// and wires the concrete classes together. The only place that knows every implementation.
/// </summary>
public sealed class AppBootstrapper : IDisposable
{
    private readonly Application _app;
    private SingleInstanceGuard? _guard;

    public AppBootstrapper(Application app) => _app = app;

    /// <summary>Returns false when this process has nothing more to do and should exit.</summary>
    public bool Run(IReadOnlyList<string> args)
    {
        var elevation = new ElevationService();

        int dump = args.ToList().IndexOf("--dump");
        if (dump >= 0)
        {
            SensorDump.Write(dump + 1 < args.Count ? args[dump + 1] : "pcpulse-dump.txt", elevation.IsElevated);
            return false;
        }

        // --no-elevate is a diagnostic mode (no ETW) that may run beside the installed copy.
        bool diagnostic = args.Contains("--no-elevate");
        if (!diagnostic)
        {
            // A second launch brings the running copy forward instead; checked before elevating to skip the UAC prompt.
            if (SingleInstanceGuard.NotifyRunning()) return false;

            // FPS (ETW) and the CPU temperature driver both need admin rights. If the user declines, run without.
            if (!elevation.IsElevated && elevation.TryRelaunchElevated(args)) return false;

            _guard = SingleInstanceGuard.TryAcquire();
            if (_guard == null)
            {
                SingleInstanceGuard.NotifyRunning();
                return false;
            }
        }

        var shell = Compose(elevation.IsElevated);
        shell.Start();
        _guard?.Listen(shell.ShowWindow);
        return true;
    }

    private ShellController Compose(bool isElevated)
    {
        var frameRate = new FrameRateMonitor(
            new EtwPresentEventSource(), new ProcessNameResolver(), ForegroundFirstSelector.CreateDefault(),
            TimeProvider.System, Environment.ProcessId);
        var monitoring = new MonitoringService(
            () => new LibreHardwareSensor(), frameRate, new ForegroundProcess(), TimeSpan.FromSeconds(1));

        var health = new HealthLog(AppPaths.HealthLogFile, HealthLog.IntervalFromEnvironment(), TimeProvider.System);
        monitoring.Tick += health.OnTick;

        var main = new MainViewModel(
            [new CpuCardViewModel(isElevated), new GpuCardViewModel(), new FpsCardViewModel()], isElevated);
        var window = new MainWindow(main);
        _app.MainWindow = window;
        var overlay = new OverlayViewModel();

        return new ShellController(
            window, main, overlay, () => new OverlayWindow(overlay),
            new TrayIcon(), new GlobalHotkeyService(), new JsonSettingsStore(AppPaths.SettingsFile),
            new SettingsDialogService(window), monitoring, new WpfUiDispatcher(_app.Dispatcher), _app.Shutdown);
    }

    public void Dispose() => _guard?.Dispose();
}
