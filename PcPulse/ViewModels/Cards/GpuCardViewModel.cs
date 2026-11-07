using PcPulse.Core.Models;
using PcPulse.Presentation;
using static PcPulse.Presentation.Formatting;

namespace PcPulse.ViewModels.Cards;

public sealed class GpuCardViewModel : MetricCardViewModel
{
    private readonly StatViewModel _load, _clock, _hotSpot, _vram, _power, _fan;

    public GpuCardViewModel()
        : base("ВИДЕОКАРТА", Palette.Gpu, "Температура · 60 с", historyMinScale: 100)
    {
        _load = AddStat("Загрузка");
        _clock = AddStat("Частота");
        _hotSpot = AddStat("Hot Spot");
        _vram = AddStat("Видеопамять");
        _power = AddStat("Мощность");
        _fan = AddStat("Вентилятор");
    }

    public override void Record(MonitorSample sample)
    {
        if (sample.Hardware.Gpu.Temperature is { } t) History.Push(t);
    }

    public override void Render(MonitorSample sample)
    {
        var gpu = sample.Hardware.Gpu;

        Subtitle = gpu.Name;
        SetMain(F(gpu.Temperature, "0", "°"), "ТЕМПЕРАТУРА", gpu.Temperature, 100, Palette.ForTemp(gpu.Temperature));
        _load.Value = F(gpu.Load, "0", " %");
        _clock.Value = F(gpu.ClockMhz, "N0", " МГц");
        _hotSpot.Value = F(gpu.HotSpot, "0", " °C");
        _vram.Value = gpu.VramUsedMb.HasValue
            ? $"{F(gpu.VramUsedMb / 1024, "0.0")} / {F(gpu.VramTotalMb / 1024, "0")} ГБ"
            : "—";
        _power.Value = F(gpu.PowerWatts, "0", " Вт");
        _fan.Value = gpu.FanRpm.HasValue
            ? F(gpu.FanRpm, "N0", " об/мин") + (gpu.FanPercent.HasValue ? $"  ({F(gpu.FanPercent, "0")} %)" : "")
            : F(gpu.FanPercent, "0", " %");
    }
}
