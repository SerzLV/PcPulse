using PcPulse.Core.Abstractions;
using PcPulse.Core.Fps;
using PcPulse.Core.Models;
using PcPulse.Core.Monitoring;

namespace PcPulse.Tests;

public class MonitoringServiceTests
{
    private sealed class FakeSensor : IHardwareSensor
    {
        public Func<HardwareSnapshot> Read { get; init; } = Samples.Hardware;
        public bool Disposed { get; private set; }
        HardwareSnapshot IHardwareSensor.Read() => Read();
        public void Dispose() => Disposed = true;
    }

    private sealed class FixedForeground(int pid) : IForegroundProcess
    {
        public int ProcessId => pid;
    }

    private static FrameRateMonitor FrameRate(FakePresentSource source) =>
        new(source, new FakeNames(new() { [7] = "game" }), ForegroundFirstSelector.CreateDefault(), TimeProvider.System, 999);

    [Fact]
    public async Task Publishes_samples_with_the_foreground_frame_rate()
    {
        var source = new FakePresentSource();
        var sensor = new FakeSensor();
        using var service = new MonitoringService(() => sensor, FrameRate(source), new FixedForeground(7), TimeSpan.FromMilliseconds(20));
        var first = new TaskCompletionSource<MonitorSample>();
        service.SampleReady += s => { if (s.Fps != null) first.TrySetResult(s); };

        service.Start();
        source.Emit(pid: 7, fps: 60, frames: 120);
        var sample = await first.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(source.Started);
        Assert.Equal("game", sample.Fps!.ProcessName);
        Assert.True(sample.FpsIsForeground);
        Assert.Equal("CPU", sample.Hardware.Cpu.Name);
    }

    [Fact]
    public async Task Keeps_ticking_and_reports_failures_when_a_read_throws()
    {
        var sensor = new FakeSensor { Read = () => throw new InvalidOperationException("датчик недоступен") };
        using var service = new MonitoringService(() => sensor, FrameRate(new FakePresentSource()), new FixedForeground(1), TimeSpan.FromMilliseconds(10));
        var failure = new TaskCompletionSource<string>();
        int ticks = 0;
        var ticked = new TaskCompletionSource();
        service.Failed += m => failure.TrySetResult(m);
        service.Tick += () => { if (Interlocked.Increment(ref ticks) >= 3) ticked.TrySetResult(); };

        service.Start();

        Assert.Equal("датчик недоступен", await failure.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        await ticked.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Dispose_stops_polling_and_releases_the_sensor()
    {
        var sensor = new FakeSensor();
        var service = new MonitoringService(() => sensor, FrameRate(new FakePresentSource()), new FixedForeground(1), TimeSpan.FromMilliseconds(10));
        var started = new TaskCompletionSource();
        service.Tick += () => started.TrySetResult();
        service.Start();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        service.Dispose();

        Assert.True(sensor.Disposed);
    }
}
