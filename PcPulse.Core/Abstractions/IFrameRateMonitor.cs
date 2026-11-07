using PcPulse.Core.Models;

namespace PcPulse.Core.Abstractions;

/// <summary>Turns present events into per-process frame rates. Thread-safe.</summary>
public interface IFrameRateMonitor : IDisposable
{
    /// <summary>Why frame tracking is unavailable, or null when it works.</summary>
    string? Error { get; }

    void Start();

    /// <summary>All processes that presented recently, busiest first.</summary>
    IReadOnlyList<FrameStats> GetAll();

    /// <summary>The process the user most likely cares about, given which one is in the foreground.</summary>
    FrameStats? GetForTarget(int foregroundProcessId);
}
