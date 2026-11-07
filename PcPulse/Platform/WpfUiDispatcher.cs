using System.Windows.Threading;
using PcPulse.Shell;

namespace PcPulse.Platform;

public sealed class WpfUiDispatcher : IUiDispatcher
{
    private readonly Dispatcher _dispatcher;

    public WpfUiDispatcher(Dispatcher dispatcher) => _dispatcher = dispatcher;

    public void Post(Action action) => _dispatcher.BeginInvoke(action);
}
