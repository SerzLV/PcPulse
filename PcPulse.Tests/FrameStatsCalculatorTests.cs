using PcPulse.Core.Fps;

namespace PcPulse.Tests;

public class FrameStatsCalculatorTests
{
    private static List<double> Even(double fps, int frames) =>
        Enumerable.Range(0, frames).Select(i => i * 1000.0 / fps).ToList();

    [Fact]
    public void Fewer_than_two_frames_gives_nothing()
    {
        Assert.Null(FrameStatsCalculator.Compute([]));
        Assert.Null(FrameStatsCalculator.Compute([5.0]));
    }

    [Theory]
    [InlineData(60)]
    [InlineData(144)]
    [InlineData(30)]
    public void Steady_rate_is_reported_with_matching_frame_time_and_low(double fps)
    {
        var timing = FrameStatsCalculator.Compute(Even(fps, (int)fps * 5))!.Value;

        Assert.Equal(fps, timing.Fps, precision: 1);
        Assert.Equal(1000 / fps, timing.FrameTimeMs, precision: 2);
        Assert.NotNull(timing.Low1Fps);
        Assert.Equal(fps, timing.Low1Fps!.Value, precision: 1);
    }

    [Fact]
    public void Low1_is_withheld_until_enough_frames()
    {
        var timing = FrameStatsCalculator.Compute(Even(60, FrameStatsCalculator.MinFramesForLow1 - 1))!.Value;

        Assert.Null(timing.Low1Fps);
    }

    [Fact]
    public void Low1_reflects_stutters_above_one_percent()
    {
        // 600 frames at 60 fps with 10 frames (1.7 %) stretched to 50 ms.
        var times = new List<double> { 0 };
        for (int i = 1; i < 600; i++) times.Add(times[^1] + (i % 60 == 0 ? 50 : 1000.0 / 60));

        var timing = FrameStatsCalculator.Compute(times)!.Value;

        Assert.Equal(20, timing.Low1Fps!.Value, precision: 1);
    }

    [Fact]
    public void Fps_only_counts_the_last_second()
    {
        // Two seconds at 30 fps, then one second at 120 fps.
        var times = Even(30, 60);
        double start = times[^1];
        times.AddRange(Enumerable.Range(1, 120).Select(i => start + i * 1000.0 / 120));

        var timing = FrameStatsCalculator.Compute(times)!.Value;

        Assert.Equal(120, timing.Fps, precision: 0);
    }
}
