using PcPulse.Core.Fps;
using PcPulse.Core.Models;

namespace PcPulse.Tests;

public class FrameRateMonitorTests
{
    private const int OwnPid = 999;

    private readonly FakePresentSource _source = new();
    private readonly ManualTimeProvider _time = new();
    private readonly FakeNames _names = new(new() { [100] = "game", [200] = "browser", [OwnPid] = "PcPulse" });
    private readonly FrameRateMonitor _monitor;

    public FrameRateMonitorTests() =>
        _monitor = new FrameRateMonitor(_source, _names, ForegroundFirstSelector.CreateDefault(), _time, OwnPid);

    [Fact]
    public void Reports_each_presenting_process_busiest_first()
    {
        _source.Emit(pid: 200, fps: 60, frames: 120);
        _source.Emit(pid: 100, fps: 144, frames: 288);

        var all = _monitor.GetAll();

        Assert.Equal(["game", "browser"], all.Select(s => s.ProcessName));
        Assert.Equal(144, all[0].Fps, precision: 0);
        Assert.Equal(60, all[1].Fps, precision: 0);
    }

    [Fact]
    public void Ignores_own_process()
    {
        _source.Emit(pid: OwnPid, fps: 60, frames: 120);

        Assert.Empty(_monitor.GetAll());
    }

    [Fact]
    public void Forgets_processes_that_stopped_presenting()
    {
        _source.Emit(pid: 100, fps: 60, frames: 120);
        _time.Advance(FrameRateMonitor.StaleAfter + TimeSpan.FromSeconds(1));

        Assert.Empty(_monitor.GetAll());
    }

    [Fact]
    public void Looks_names_up_again_after_a_process_is_forgotten()
    {
        _source.Emit(pid: 100, fps: 60, frames: 120);
        _monitor.GetAll();
        _monitor.GetAll();
        Assert.Equal(1, _names.Lookups);

        _time.Advance(FrameRateMonitor.StaleAfter + TimeSpan.FromSeconds(1));
        _monitor.GetAll();
        _source.Emit(pid: 100, fps: 60, frames: 120, startMs: 20_000);
        _monitor.GetAll();

        Assert.Equal(2, _names.Lookups);
    }

    [Fact]
    public void Keeps_only_the_recent_history_window()
    {
        // 20 s at 60 fps: frames older than the 10 s window must not drag the 1% low or grow memory.
        _source.Emit(pid: 100, fps: 60, frames: 1200);

        var stats = Assert.Single(_monitor.GetAll());
        Assert.Equal(60, stats.Low1Fps!.Value, precision: 0);
    }

    [Fact]
    public void Uses_the_api_with_more_frames_when_a_process_uses_both()
    {
        _source.Emit(pid: 100, fps: 30, frames: 60, api: GraphicsApi.D3D9);
        _source.Emit(pid: 100, fps: 90, frames: 180, api: GraphicsApi.Dxgi);

        var stats = Assert.Single(_monitor.GetAll());
        Assert.Equal(90, stats.Fps, precision: 0);
    }

    [Fact]
    public void Prefers_the_foreground_process()
    {
        _source.Emit(pid: 100, fps: 144, frames: 288);
        _source.Emit(pid: 200, fps: 60, frames: 120);

        Assert.Equal("browser", _monitor.GetForTarget(foregroundProcessId: 200)!.ProcessName);
        Assert.Equal("game", _monitor.GetForTarget(foregroundProcessId: 12345)!.ProcessName);
    }

    [Fact]
    public void Forwards_start_error_and_dispose_to_the_source()
    {
        _source.Error = "нужны права администратора";
        _monitor.Start();

        Assert.True(_source.Started);
        Assert.Equal("нужны права администратора", _monitor.Error);

        _monitor.Dispose();
        Assert.True(_source.Disposed);
    }
}
