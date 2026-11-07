using PcPulse.Core.Abstractions;

namespace PcPulse.Infrastructure.Windows;

/// <summary>The process owning the current foreground window.</summary>
public sealed class ForegroundProcess : IForegroundProcess
{
    public int ProcessId
    {
        get
        {
            NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out int pid);
            return pid;
        }
    }
}
