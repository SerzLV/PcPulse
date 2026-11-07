using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using PcPulse.Core.Abstractions;

namespace PcPulse.Infrastructure.Windows;

public sealed class ElevationService : IElevation
{
    public bool IsElevated
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public bool TryRelaunchElevated(IReadOnlyList<string> args)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!)
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = string.Join(" ", args.Select(a => $"\"{a}\"")),
            });
            return true;
        }
        catch (Win32Exception)
        {
            return false; // the user declined the UAC prompt
        }
    }
}
