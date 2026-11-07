using LibreHardwareMonitor.Hardware;
using PcPulse.Core.Abstractions;
using PcPulse.Core.Models;

namespace PcPulse.Infrastructure.Hardware;

/// <summary>
/// Reads sensors through LibreHardwareMonitor. CPU temperature, clock and power on Ryzen need the PawnIO driver
/// and administrator rights; without them those sensors exist but read 0, which is reported as null.
/// </summary>
public sealed class LibreHardwareSensor : IHardwareSensor
{
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
    };

    public LibreHardwareSensor() => _computer.Open();

    public HardwareSnapshot Read()
    {
        foreach (var hw in _computer.Hardware)
        {
            hw.Update();
            foreach (var sub in hw.SubHardware) sub.Update();
        }
        return new HardwareSnapshot(ReadCpu(), ReadGpu(), ReadMemory());
    }

    /// <summary>Every sensor as "hardware | type | name = value", for diagnostics.</summary>
    public IEnumerable<string> DescribeAll() =>
        from hw in _computer.Hardware
        from s in AllSensors([hw])
        select $"{hw.HardwareType} {hw.Name} | {s.SensorType} | {s.Name} = {s.Value?.ToString("0.##") ?? "null"}";

    private CpuReading ReadCpu()
    {
        var cpu = Of(HardwareType.Cpu);
        return new CpuReading(
            Name: cpu.FirstOrDefault()?.Name ?? "CPU",
            Temperature: Positive(Get(cpu, SensorType.Temperature,
                "Core (Tctl/Tdie)", "Core (Tdie)", "Core (Tctl)", "CPU Package", "Core Max", "Core Average")),
            Load: Get(cpu, SensorType.Load, "CPU Total"),
            // "Effective" clocks average in time spent asleep (hundreds of MHz at idle); show the running clock instead.
            ClockMhz: Positive(Get(cpu, SensorType.Clock, "Cores (Average)")) ?? Average(cpu, SensorType.Clock, "Core #"),
            PowerWatts: Positive(Get(cpu, SensorType.Power, "Package", "CPU Package")));
    }

    private GpuReading ReadGpu()
    {
        var gpus = Of(HardwareType.GpuNvidia);
        if (gpus.Length == 0) gpus = Of(HardwareType.GpuAmd);
        if (gpus.Length == 0) gpus = Of(HardwareType.GpuIntel);
        var gpu = gpus.Take(1).ToArray();

        return new GpuReading(
            Name: gpu.FirstOrDefault()?.Name ?? "GPU",
            Temperature: Get(gpu, SensorType.Temperature, "GPU Core"),
            HotSpot: Get(gpu, SensorType.Temperature, "GPU Hot Spot"),
            Load: Get(gpu, SensorType.Load, "GPU Core", "D3D 3D"),
            ClockMhz: Get(gpu, SensorType.Clock, "GPU Core"),
            PowerWatts: Get(gpu, SensorType.Power, "GPU Package", "GPU Power"),
            FanRpm: Get(gpu, SensorType.Fan, "GPU Fan"),
            FanPercent: Get(gpu, SensorType.Control, "GPU Fan"),
            VramUsedMb: Get(gpu, SensorType.SmallData, "GPU Memory Used", "D3D Dedicated Memory Used"),
            VramTotalMb: Get(gpu, SensorType.SmallData, "GPU Memory Total"));
    }

    private MemoryReading ReadMemory()
    {
        // LHM also exposes a "Virtual Memory" (commit) hardware; physical RAM is "Total Memory".
        var mem = Of(HardwareType.Memory)
            .Where(h => !h.Name.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var used = Get(mem, SensorType.Data, "Memory Used");
        var free = Get(mem, SensorType.Data, "Memory Available");
        return new MemoryReading(Get(mem, SensorType.Load, "Memory"), used, used + free);
    }

    public void Dispose() => _computer.Close();

    private IHardware[] Of(HardwareType type) => _computer.Hardware.Where(h => h.HardwareType == type).ToArray();

    private static IEnumerable<ISensor> AllSensors(IEnumerable<IHardware> hws) =>
        hws.SelectMany(h => h.Sensors.Concat(h.SubHardware.SelectMany(s => s.Sensors)));

    // Exact name match first (in priority order), then prefix match.
    private static float? Get(IEnumerable<IHardware> hws, SensorType type, params string[] names)
    {
        var sensors = AllSensors(hws).Where(s => s.SensorType == type && s.Value.HasValue).ToList();
        foreach (var n in names)
            if (sensors.FirstOrDefault(s => s.Name.Equals(n, StringComparison.OrdinalIgnoreCase)) is { } exact)
                return exact.Value;
        foreach (var n in names)
            if (sensors.FirstOrDefault(s => s.Name.StartsWith(n, StringComparison.OrdinalIgnoreCase)) is { } prefix)
                return prefix.Value;
        return null;
    }

    private static float? Positive(float? v) => v > 0 ? v : null;

    private static float? Average(IEnumerable<IHardware> hws, SensorType type, string prefix)
    {
        var values = AllSensors(hws)
            .Where(s => s.SensorType == type && s.Value is > 0 && s.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && !s.Name.Contains("Effective", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Value!.Value)
            .ToList();
        return values.Count > 0 ? values.Average() : null;
    }
}
