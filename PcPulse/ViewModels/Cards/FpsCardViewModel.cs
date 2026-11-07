using PcPulse.Core.Models;
using PcPulse.Presentation;
using static PcPulse.Presentation.Formatting;

namespace PcPulse.ViewModels.Cards;

public sealed class FpsCardViewModel : MetricCardViewModel
{
    /// <summary>The ring is full at this frame rate unless the game runs faster.</summary>
    private const double GaugeFloor = 144;

    private readonly StatViewModel _frameTime, _low1, _source;
    private string? _lastProcess;

    public FpsCardViewModel()
        : base("FPS", Palette.Fps, "FPS · 60 с", historyMinScale: 60)
    {
        _frameTime = AddStat("Время кадра");
        _low1 = AddStat("1% low");
        _source = AddStat("Источник");
        SetMain("—", "ожидание кадров", null, GaugeFloor, Palette.Muted);
    }

    public override void Record(MonitorSample sample)
    {
        if (sample.Fps is not { } fps) return;

        // A different game starts a fresh chart.
        if (fps.ProcessName != _lastProcess)
        {
            History.Clear();
            _lastProcess = fps.ProcessName;
        }
        History.Push(fps.Fps);
    }

    public override void Render(MonitorSample sample)
    {
        if (sample.Fps is { } fps)
        {
            Subtitle = fps.ProcessName + ".exe";
            SetMain(F(fps.Fps, "0"), "КАДРОВ / С", fps.Fps, Math.Max(GaugeFloor, fps.Fps), Palette.ForFps(fps.Fps));
            _frameTime.Value = F(fps.FrameTimeMs, "0.0", " мс");
            _low1.Value = F(fps.Low1Fps, "0", " fps");
            _source.Value = sample.FpsIsForeground ? "активное окно" : "фоновый процесс";
            Hint = null;
        }
        else
        {
            bool broken = sample.FpsError != null;
            Subtitle = broken ? "FPS недоступен" : "Запустите игру";
            SetMain("—", broken ? "нет доступа" : "нет кадров", null, GaugeFloor, Palette.Muted);
            _frameTime.Value = _low1.Value = _source.Value = "—";
            Hint = broken ? "FPS: " + sample.FpsError : null;
        }
    }
}
