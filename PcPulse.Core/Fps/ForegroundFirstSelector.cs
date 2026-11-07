using PcPulse.Core.Abstractions;
using PcPulse.Core.Models;

namespace PcPulse.Core.Fps;

/// <summary>
/// Shows the foreground window's frame rate; if it isn't presenting (e.g. you're looking at PC Pulse itself),
/// falls back to the busiest process that isn't system plumbing.
/// </summary>
public sealed class ForegroundFirstSelector : IFpsTargetSelector
{
    private readonly HashSet<string> _ignored;

    public ForegroundFirstSelector(IEnumerable<string> ignoredProcessNames) =>
        _ignored = new HashSet<string>(ignoredProcessNames, StringComparer.OrdinalIgnoreCase);

    /// <summary>Ignores the compositor, the client/server runtime and PC Pulse itself.</summary>
    public static ForegroundFirstSelector CreateDefault() => new(["dwm", "csrss", "PcPulse"]);

    public FrameStats? Select(IReadOnlyList<FrameStats> busiestFirst, int foregroundProcessId) =>
        busiestFirst.FirstOrDefault(s => s.ProcessId == foregroundProcessId)
        ?? busiestFirst.FirstOrDefault(s => !_ignored.Contains(s.ProcessName));
}
