using PcPulse.Core.Models;
using PcPulse.Presentation;
using static PcPulse.Presentation.Formatting;

namespace PcPulse.ViewModels.Cards;

public sealed class CpuCardViewModel : MetricCardViewModel
{
    private readonly bool _isElevated;
    private readonly StatViewModel _load, _clock, _power, _ram;

    /// <param name="isElevated">Decides which hint explains a missing temperature.</param>
    public CpuCardViewModel(bool isElevated)
        : base("ПРОЦЕССОР", Palette.Cpu, "Температура · 60 с", historyMinScale: 100)
    {
        _isElevated = isElevated;
        _load = AddStat("Загрузка");
        _clock = AddStat("Частота");
        _power = AddStat("Мощность");
        _ram = AddStat("ОЗУ");
    }

    public override void Record(MonitorSample sample)
    {
        if (sample.Hardware.Cpu.Temperature is { } t) History.Push(t);
    }

    public override void Render(MonitorSample sample)
    {
        var cpu = sample.Hardware.Cpu;
        var mem = sample.Hardware.Memory;

        Subtitle = cpu.Name;
        SetMain(F(cpu.Temperature, "0", "°"), cpu.Temperature.HasValue ? "ТЕМПЕРАТУРА" : "нет датчика",
                cpu.Temperature, 100, Palette.ForTemp(cpu.Temperature));
        _load.Value = F(cpu.Load, "0", " %");
        _clock.Value = F(cpu.ClockMhz, "N0", " МГц");
        _power.Value = F(cpu.PowerWatts, "0", " Вт");
        _ram.Value = mem.UsedGb.HasValue ? $"{F(mem.UsedGb, "0.0")} / {F(mem.TotalGb, "0")} ГБ" : F(mem.Load, "0", " %");

        Hint = cpu.Temperature.HasValue ? null
            : !_isElevated ? "Температура CPU доступна только при запуске от администратора."
            : "Для температуры Ryzen нужен драйвер PawnIO:  winget install namazso.PawnIO  — затем перезапустите PC Pulse.";
    }
}
