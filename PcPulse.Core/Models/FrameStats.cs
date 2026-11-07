namespace PcPulse.Core.Models;

/// <summary>Frame-rate figures for one process.</summary>
public sealed record FrameStats(int ProcessId, string ProcessName, double Fps, double FrameTimeMs, double? Low1Fps);

/// <summary>Frame-rate figures computed from present timestamps, before they are tied to a process.</summary>
public readonly record struct FrameTiming(double Fps, double FrameTimeMs, double? Low1Fps);

/// <summary>Graphics API a frame was presented through.</summary>
public enum GraphicsApi { Dxgi, D3D9 }

/// <summary>A single Present call observed for a process; <see cref="TimestampMs"/> is on the source's own clock.</summary>
public readonly record struct PresentEvent(int ProcessId, GraphicsApi Api, double TimestampMs);
