using System.Globalization;
using PcPulse.Core.Models;

namespace PcPulse.Presentation;

/// <summary>Number and text formatting for the UI (Russian locale: "1 234", "3,5").</summary>
public static class Formatting
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>Formats a nullable number, "—" when missing.</summary>
    public static string F(double? value, string format, string unit = "") =>
        value.HasValue ? value.Value.ToString(format, Ru) + unit : "—";

    /// <summary>Hover text for the tray icon.</summary>
    public static string TrayTooltip(MonitorSample sample)
    {
        var hw = sample.Hardware;
        string text = $"PC Pulse\nCPU {F(hw.Cpu.Temperature, "0", "°")} · GPU {F(hw.Gpu.Temperature, "0", "°")}";
        return sample.Fps is { } fps ? $"{text}\n{fps.ProcessName}: {F(fps.Fps, "0")} fps" : text;
    }
}
