using System.Windows;
using PcPulse.Composition;

namespace PcPulse;

public partial class App : Application
{
    private readonly AppBootstrapper _bootstrapper;

    public App() => _bootstrapper = new AppBootstrapper(this);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (!_bootstrapper.Run(e.Args)) Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _bootstrapper.Dispose();
        base.OnExit(e);
    }
}
