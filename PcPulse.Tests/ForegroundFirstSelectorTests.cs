using PcPulse.Core.Fps;
using static PcPulse.Tests.Samples;

namespace PcPulse.Tests;

public class ForegroundFirstSelectorTests
{
    private readonly ForegroundFirstSelector _selector = ForegroundFirstSelector.CreateDefault();

    [Fact]
    public void Picks_the_foreground_process_even_if_slower()
    {
        var all = new[] { Stats(1, "game", 144), Stats(2, "chrome", 60) };

        Assert.Equal("chrome", _selector.Select(all, foregroundProcessId: 2)!.ProcessName);
    }

    [Fact]
    public void Falls_back_to_the_busiest_non_system_process()
    {
        var all = new[] { Stats(1, "dwm", 240), Stats(2, "PcPulse", 120), Stats(3, "game", 90) };

        Assert.Equal("game", _selector.Select(all, foregroundProcessId: 42)!.ProcessName);
    }

    [Fact]
    public void Returns_nothing_when_only_system_processes_present()
    {
        var all = new[] { Stats(1, "DWM", 60), Stats(2, "csrss", 60) };

        Assert.Null(_selector.Select(all, foregroundProcessId: 42));
    }
}
