namespace PcPulse.Core.Models;

/// <summary>Processor readings. Null means the sensor is missing or unreadable (e.g. no driver).</summary>
public sealed record CpuReading(string Name, float? Temperature, float? Load, float? ClockMhz, float? PowerWatts);

/// <summary>Graphics card readings. Null means the sensor is missing or unreadable.</summary>
public sealed record GpuReading(
    string Name, float? Temperature, float? HotSpot, float? Load, float? ClockMhz, float? PowerWatts,
    float? FanRpm, float? FanPercent, float? VramUsedMb, float? VramTotalMb);

/// <summary>Physical memory readings.</summary>
public sealed record MemoryReading(float? Load, float? UsedGb, float? TotalGb);

/// <summary>One read of all hardware sensors.</summary>
public sealed record HardwareSnapshot(CpuReading Cpu, GpuReading Gpu, MemoryReading Memory);
