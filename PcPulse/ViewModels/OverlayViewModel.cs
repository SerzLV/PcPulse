using System.Windows.Media;
using PcPulse.Core.Models;
using PcPulse.Presentation;
using static PcPulse.Presentation.Formatting;

namespace PcPulse.ViewModels;

/// <summary>The three lines of the in-game overlay: FPS, CPU and GPU.</summary>
public sealed class OverlayViewModel : ObservableObject
{
    private string _fpsText = "—";
    private Color _fpsColor = Palette.Muted;
    private string _frameText = "";
    private string _cpuTempText = "—";
    private Color _cpuTempColor = Palette.Muted;
    private string _cpuLoadText = "—";
    private string _gpuTempText = "—";
    private Color _gpuTempColor = Palette.Muted;
    private string _gpuLoadText = "—";

    /// <summary>Raised after every update; the view uses it to stay on top of fullscreen games.</summary>
    public event Action? Updated;

    public string FpsText { get => _fpsText; private set => Set(ref _fpsText, value); }
    public Color FpsColor { get => _fpsColor; private set => Set(ref _fpsColor, value); }
    public string FrameText { get => _frameText; private set => Set(ref _frameText, value); }
    public string CpuTempText { get => _cpuTempText; private set => Set(ref _cpuTempText, value); }
    public Color CpuTempColor { get => _cpuTempColor; private set => Set(ref _cpuTempColor, value); }
    public string CpuLoadText { get => _cpuLoadText; private set => Set(ref _cpuLoadText, value); }
    public string GpuTempText { get => _gpuTempText; private set => Set(ref _gpuTempText, value); }
    public Color GpuTempColor { get => _gpuTempColor; private set => Set(ref _gpuTempColor, value); }
    public string GpuLoadText { get => _gpuLoadText; private set => Set(ref _gpuLoadText, value); }

    public void Update(MonitorSample sample)
    {
        var fps = sample.Fps;
        var cpu = sample.Hardware.Cpu;
        var gpu = sample.Hardware.Gpu;

        FpsText = F(fps?.Fps, "0");
        FpsColor = fps == null ? Palette.Muted : Palette.ForFps(fps.Fps);
        FrameText = fps == null ? "" : F(fps.FrameTimeMs, "0.0", " ms");

        CpuTempText = F(cpu.Temperature, "0", "°C");
        CpuTempColor = Palette.ForTemp(cpu.Temperature);
        CpuLoadText = F(cpu.Load, "0", "%");

        GpuTempText = F(gpu.Temperature, "0", "°C");
        GpuTempColor = Palette.ForTemp(gpu.Temperature);
        GpuLoadText = F(gpu.Load, "0", "%");

        Updated?.Invoke();
    }
}
