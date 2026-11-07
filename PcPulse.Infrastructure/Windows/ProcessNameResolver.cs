using System.Diagnostics;
using PcPulse.Core.Abstractions;

namespace PcPulse.Infrastructure.Windows;

public sealed class ProcessNameResolver : IProcessNameResolver
{
    public string GetName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return $"PID {processId}";
        }
    }
}
