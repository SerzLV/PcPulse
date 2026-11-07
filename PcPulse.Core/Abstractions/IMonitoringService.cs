using PcPulse.Core.Models;

namespace PcPulse.Core.Abstractions;

/// <summary>Polls sensors and frame rate on a background thread. Events fire on that thread.</summary>
public interface IMonitoringService : IDisposable
{
    /// <summary>A fresh reading is available.</summary>
    event Action<MonitorSample>? SampleReady;

    /// <summary>Reading failed; the argument is a short reason.</summary>
    event Action<string>? Failed;

    /// <summary>Raised once per poll, whether or not it succeeded. Use for periodic housekeeping.</summary>
    event Action? Tick;

    void Start();
}
