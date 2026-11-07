using System.Windows.Input;
using System.Windows.Media;
using PcPulse.Core.Models;
using PcPulse.Core.Settings;
using PcPulse.Presentation;
using PcPulse.ViewModels.Cards;

namespace PcPulse.ViewModels;

/// <summary>
/// State of the main window. Commands only announce intent through events; what minimizing or closing means
/// (tray or not) is the shell's decision.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly bool _isElevated;
    private MonitorSample? _last;
    private bool _isViewVisible;
    private string _systemText = "Подключаюсь к датчикам…";
    private string _statusText = "";
    private Color _statusColor = Palette.Muted;
    private bool _isOverlayOn;
    private string _overlayButtonText = "Оверлей";
    private string? _hotkeyWarning;

    public MainViewModel(IReadOnlyList<MetricCardViewModel> cards, bool isElevated)
    {
        Cards = cards;
        _isElevated = isElevated;
        OpenSettingsCommand = new RelayCommand(() => SettingsRequested?.Invoke());
        MinimizeCommand = new RelayCommand(() => MinimizeRequested?.Invoke());
        CloseCommand = new RelayCommand(() => CloseRequested?.Invoke());
        UpdateStatus();
    }

    public event Action? SettingsRequested;
    public event Action? MinimizeRequested;
    public event Action? CloseRequested;

    public IReadOnlyList<MetricCardViewModel> Cards { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand MinimizeCommand { get; }
    public ICommand CloseCommand { get; }

    public string SystemText { get => _systemText; private set => Set(ref _systemText, value); }
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public Color StatusColor { get => _statusColor; private set => Set(ref _statusColor, value); }
    public string OverlayButtonText { get => _overlayButtonText; private set => Set(ref _overlayButtonText, value); }

    public bool IsOverlayOn
    {
        get => _isOverlayOn;
        set => Set(ref _isOverlayOn, value);
    }

    /// <summary>Set by the view. Cards are only redrawn while it's true, and catch up when it turns true.</summary>
    public bool IsViewVisible
    {
        get => _isViewVisible;
        set
        {
            if (Set(ref _isViewVisible, value) && value && _last != null) Render(_last);
        }
    }

    /// <summary>A hotkey that couldn't be registered, shown in the status line; null clears it.</summary>
    public string? HotkeyWarning
    {
        get => _hotkeyWarning;
        set
        {
            if (Set(ref _hotkeyWarning, value)) UpdateStatus();
        }
    }

    public void SetOverlayHotkey(Hotkey hotkey) =>
        OverlayButtonText = hotkey.IsEmpty ? "Оверлей" : "Оверлей  " + hotkey.ToString().Replace(" ", "");

    public void OnSample(MonitorSample sample)
    {
        _last = sample;
        foreach (var card in Cards) card.Record(sample);
        if (_isViewVisible) Render(sample);
    }

    public void ReportFailure(string message) => StatusText = "Ошибка датчиков: " + message;

    private void Render(MonitorSample sample)
    {
        SystemText = $"{sample.Hardware.Cpu.Name}  ·  {sample.Hardware.Gpu.Name}";
        foreach (var card in Cards) card.Render(sample);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        bool fpsOk = _last?.FpsError == null;
        StatusColor = _isElevated && fpsOk && _hotkeyWarning == null ? Palette.Good : Palette.Warm;
        StatusText = _hotkeyWarning
            ?? (_isElevated
                ? (fpsOk ? "Права администратора · FPS-трекинг активен (DirectX 9–12)" : "FPS-трекинг не запустился")
                : "Без прав администратора — FPS и температура CPU недоступны");
    }
}
