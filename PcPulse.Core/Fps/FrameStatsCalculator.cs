using PcPulse.Core.Models;

namespace PcPulse.Core.Fps;

/// <summary>Pure frame-rate math over present timestamps.</summary>
public static class FrameStatsCalculator
{
    /// <summary>Below this many frames there aren't enough intervals for a meaningful 1% low.</summary>
    public const int MinFramesForLow1 = 30;

    private const double WindowMs = 1000;

    /// <summary>
    /// FPS over the last second, its frame time, and the 1% low over all given frames.
    /// </summary>
    /// <param name="timesMs">Present timestamps in milliseconds, ascending.</param>
    /// <returns>Null when there are fewer than two frames.</returns>
    public static FrameTiming? Compute(IReadOnlyList<double> timesMs)
    {
        if (timesMs.Count < 2) return null;

        double last = timesMs[^1];
        int first = 0;
        while (timesMs[first] <= last - WindowMs) first++;
        int count = timesMs.Count - first;
        double span = last - timesMs[first];
        double fps = count >= 3 && span > 0 ? (count - 1) * 1000.0 / span : count;

        return new FrameTiming(fps, fps > 0 ? 1000.0 / fps : 0, Low1(timesMs));
    }

    /// <summary>FPS equivalent of the 99th-percentile frame time.</summary>
    private static double? Low1(IReadOnlyList<double> timesMs)
    {
        if (timesMs.Count < MinFramesForLow1) return null;

        var deltas = new double[timesMs.Count - 1];
        for (int i = 1; i < timesMs.Count; i++) deltas[i - 1] = timesMs[i] - timesMs[i - 1];
        Array.Sort(deltas);
        double p99 = deltas[(int)Math.Floor((deltas.Length - 1) * 0.99)];
        return p99 > 0 ? 1000.0 / p99 : null;
    }
}
