using PcPulse.Core.Abstractions;
using PcPulse.Core.Models;

namespace PcPulse.Core.Fps;

/// <summary>
/// Keeps the last <see cref="HistoryMs"/> of present timestamps per process and graphics API,
/// and turns them into <see cref="FrameStats"/> on request.
/// </summary>
public sealed class FrameRateMonitor : IFrameRateMonitor
{
    public const double HistoryMs = 10_000;

    /// <summary>A process that hasn't presented for this long is forgotten.</summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(3);

    private sealed class Track
    {
        public readonly Queue<double> Times = new();
        public DateTimeOffset LastSeen;
    }

    private readonly IPresentEventSource _source;
    private readonly IProcessNameResolver _nameResolver;
    private readonly IFpsTargetSelector _selector;
    private readonly TimeProvider _time;
    private readonly int _ownProcessId;

    private readonly object _lock = new();
    private readonly Dictionary<(int ProcessId, GraphicsApi Api), Track> _tracks = new();
    private readonly Dictionary<int, string> _names = new();

    /// <param name="ownProcessId">Excluded from tracking, since our own UI presents frames too.</param>
    public FrameRateMonitor(IPresentEventSource source, IProcessNameResolver nameResolver,
                            IFpsTargetSelector selector, TimeProvider time, int ownProcessId)
    {
        _source = source;
        _nameResolver = nameResolver;
        _selector = selector;
        _time = time;
        _ownProcessId = ownProcessId;
        _source.Presented += OnPresented;
    }

    public string? Error => _source.Error;

    public void Start() => _source.Start();

    private void OnPresented(PresentEvent e)
    {
        if (e.ProcessId == _ownProcessId) return;

        lock (_lock)
        {
            if (!_tracks.TryGetValue((e.ProcessId, e.Api), out var track))
                _tracks[(e.ProcessId, e.Api)] = track = new Track();
            track.Times.Enqueue(e.TimestampMs);
            while (track.Times.Peek() < e.TimestampMs - HistoryMs) track.Times.Dequeue();
            track.LastSeen = _time.GetUtcNow();
        }
    }

    public IReadOnlyList<FrameStats> GetAll()
    {
        var result = new List<FrameStats>();
        lock (_lock)
        {
            ForgetStale();

            // A process can show up under both APIs; keep whichever it presents through more.
            foreach (var process in _tracks.GroupBy(kv => kv.Key.ProcessId))
            {
                FrameTiming? best = null;
                foreach (var (_, track) in process)
                    if (FrameStatsCalculator.Compute(track.Times.ToArray()) is { } t && (best is null || t.Fps > best.Value.Fps))
                        best = t;

                if (best is { } timing)
                    result.Add(new FrameStats(process.Key, NameOf(process.Key), timing.Fps, timing.FrameTimeMs, timing.Low1Fps));
            }
        }
        result.Sort((a, b) => b.Fps.CompareTo(a.Fps));
        return result;
    }

    public FrameStats? GetForTarget(int foregroundProcessId) => _selector.Select(GetAll(), foregroundProcessId);

    private void ForgetStale()
    {
        var now = _time.GetUtcNow();
        foreach (var key in _tracks.Where(kv => now - kv.Value.LastSeen > StaleAfter).Select(kv => kv.Key).ToList())
            _tracks.Remove(key);

        // Dropping names together with their tracks keeps the cache bounded and stops a reused PID
        // from showing the previous process's name.
        foreach (int pid in _names.Keys.Where(pid => !_tracks.Keys.Any(k => k.ProcessId == pid)).ToList())
            _names.Remove(pid);
    }

    private string NameOf(int processId)
    {
        if (!_names.TryGetValue(processId, out var name))
            _names[processId] = name = _nameResolver.GetName(processId);
        return name;
    }

    public void Dispose()
    {
        _source.Presented -= OnPresented;
        _source.Dispose();
    }
}
