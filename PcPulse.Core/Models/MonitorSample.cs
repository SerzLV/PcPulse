namespace PcPulse.Core.Models;

/// <summary>Everything the UI shows for one poll.</summary>
/// <param name="Fps">Frame rate of the target process, or null when nothing is presenting.</param>
/// <param name="FpsIsForeground">Whether <paramref name="Fps"/> belongs to the foreground window.</param>
/// <param name="FpsError">Why frame tracking is unavailable, or null when it works.</param>
public sealed record MonitorSample(HardwareSnapshot Hardware, FrameStats? Fps, bool FpsIsForeground, string? FpsError);
