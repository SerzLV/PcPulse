using PcPulse.Core.Abstractions;
using PcPulse.Core.Models;

namespace PcPulse.Tests;

internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

internal sealed class FakePresentSource : IPresentEventSource
{
    public event Action<PresentEvent>? Presented;
    public string? Error { get; set; }
    public bool Started { get; private set; }
    public bool Disposed { get; private set; }

    public void Start() => Started = true;

    public void Dispose() => Disposed = true;

    /// <summary>Presents <paramref name="frames"/> evenly spaced frames at <paramref name="fps"/>, starting at <paramref name="startMs"/>.</summary>
    public void Emit(int pid, double fps, int frames, double startMs = 0, GraphicsApi api = GraphicsApi.Dxgi)
    {
        for (int i = 0; i < frames; i++)
            Presented?.Invoke(new PresentEvent(pid, api, startMs + i * 1000.0 / fps));
    }
}

internal sealed class FakeNames : IProcessNameResolver
{
    private readonly Dictionary<int, string> _names;

    public FakeNames(Dictionary<int, string> names) => _names = names;

    public int Lookups { get; private set; }

    public string GetName(int processId)
    {
        Lookups++;
        return _names.TryGetValue(processId, out var n) ? n : $"PID {processId}";
    }
}

internal static class Samples
{
    public static FrameStats Stats(int pid, string name, double fps) => new(pid, name, fps, 1000 / fps, null);

    public static HardwareSnapshot Hardware() => new(
        new CpuReading("CPU", 50, 10, 3600, 40),
        new GpuReading("GPU", 45, 55, 5, 1800, 20, 0, 0, 1024, 12288),
        new MemoryReading(40, 13, 32));
}
