using System.ComponentModel;
using PcPulse.Core.Abstractions;
using PcPulse.Core.Models;
using PcPulse.Core.Settings;
using PcPulse.Presentation;
using PcPulse.ViewModels;

namespace PcPulse.Shell;

/// <summary>
/// Application behaviour: what the tray, hotkeys, window buttons and settings do, and how the app starts and stops.
/// Every UI piece is reached through an interface, so the policy lives here and the views stay passive.
/// </summary>
public sealed class ShellController
{
    private readonly IMainWindowView _window;
    private readonly MainViewModel _main;
    private readonly OverlayViewModel _overlayModel;
    private readonly Func<IOverlayView> _createOverlay;
    private readonly ITrayIcon _tray;
    private readonly IHotkeyService _hotkeys;
    private readonly ISettingsStore _store;
    private readonly ISettingsDialogService _dialogs;
    private readonly IMonitoringService _monitoring;
    private readonly IUiDispatcher _ui;
    private readonly Action _shutdown;

    private AppSettings _settings;
    private IOverlayView? _overlay;
    private bool _exiting;
    private bool _tornDown;

    public ShellController(
        IMainWindowView window, MainViewModel main, OverlayViewModel overlayModel, Func<IOverlayView> createOverlay,
        ITrayIcon tray, IHotkeyService hotkeys, ISettingsStore store, ISettingsDialogService dialogs,
        IMonitoringService monitoring, IUiDispatcher ui, Action shutdown)
    {
        _window = window;
        _main = main;
        _overlayModel = overlayModel;
        _createOverlay = createOverlay;
        _tray = tray;
        _hotkeys = hotkeys;
        _store = store;
        _dialogs = dialogs;
        _monitoring = monitoring;
        _ui = ui;
        _shutdown = shutdown;
        _settings = store.Load();
    }

    public void Start()
    {
        _monitoring.SampleReady += sample => _ui.Post(() => OnSample(sample));
        _monitoring.Failed += message => _ui.Post(() => _main.ReportFailure(message));

        _tray.OpenRequested += ShowWindow;
        _tray.OverlayToggleRequested += ToggleOverlay;
        _tray.SettingsRequested += OpenSettings;
        _tray.ExitRequested += Exit;

        _main.SettingsRequested += OpenSettings;
        _main.MinimizeRequested += MinimizeWindow;
        _main.CloseRequested += _window.Close;
        _main.PropertyChanged += OnMainPropertyChanged;

        _window.Minimized += (_, _) => { if (_settings.MinimizeToTray) HideWindow(); };
        _window.Closing += OnWindowClosing;
        _window.Closed += (_, _) => TearDown();

        var failed = RegisterHotkeys(_settings);
        if (failed.Count > 0)
            _main.HotkeyWarning = $"Горячая клавиша {string.Join(", ", failed)} занята другой программой — смените в настройках ⚙";
        _main.IsOverlayOn = _settings.OverlayVisible;
        UpdateHotkeyLabels();

        _monitoring.Start();
        if (!_settings.StartInTray) ShowWindow();
    }

    private void OnSample(MonitorSample sample)
    {
        _main.OnSample(sample);
        _overlayModel.Update(sample);
        _tray.SetTooltip(Formatting.TrayTooltip(sample));
    }

    // ---- Main window ----

    public void ShowWindow() => _window.ShowAndActivate();

    private void HideWindow()
    {
        _window.Hide();
        _tray.ShowHiddenHintOnce();
    }

    private void ToggleWindow()
    {
        if (_window.IsInFront) HideWindow();
        else ShowWindow();
    }

    private void MinimizeWindow()
    {
        if (_settings.MinimizeToTray) HideWindow();
        else _window.Minimize();
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_exiting) return;
        if (_settings.CloseToTray)
        {
            e.Cancel = true;
            HideWindow();
            return;
        }
        _exiting = true;
    }

    private void Exit()
    {
        _exiting = true;
        _window.Close();
        TearDown(); // the window may never have been shown (start in tray), so don't rely on Closed alone
    }

    private void TearDown()
    {
        if (_tornDown) return;
        _tornDown = true;
        _hotkeys.Dispose();
        _tray.Dispose();
        _overlay?.Close();
        _monitoring.Dispose();
        _shutdown();
    }

    // ---- Overlay ----

    private void ToggleOverlay() => _main.IsOverlayOn = !_main.IsOverlayOn;

    private void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsOverlayOn)) ApplyOverlay();
    }

    private void ApplyOverlay()
    {
        bool on = _main.IsOverlayOn;
        if (on)
        {
            _overlay ??= _createOverlay();
            _overlay.SetCorner(_settings.OverlayCorner);
            _overlay.Show();
        }
        else
        {
            _overlay?.Hide();
        }

        if (_settings.OverlayVisible != on)
        {
            _settings.OverlayVisible = on;
            _store.Save(_settings);
        }
        UpdateHotkeyLabels();
    }

    // ---- Hotkeys & settings ----

    /// <summary>Registers both hotkeys; returns the ones another program already holds.</summary>
    private List<string> RegisterHotkeys(AppSettings settings)
    {
        _hotkeys.UnregisterAll();
        var failed = new List<string>();
        if (!_hotkeys.Register(settings.OverlayHotkey, ToggleOverlay)) failed.Add(settings.OverlayHotkey.ToString());
        if (!_hotkeys.Register(settings.WindowHotkey, ToggleWindow)) failed.Add(settings.WindowHotkey.ToString());
        return failed;
    }

    private void UpdateHotkeyLabels()
    {
        _main.SetOverlayHotkey(_settings.OverlayHotkey);
        _tray.SetOverlayState(_main.IsOverlayOn, _settings.OverlayHotkey);
    }

    private void OpenSettings()
    {
        if (_dialogs.IsOpen)
        {
            _dialogs.Activate();
            return;
        }
        ShowWindow();

        // Global hotkeys swallow their keys system-wide, so release them while the user is typing new ones.
        _hotkeys.UnregisterAll();
        if (!_dialogs.ShowDialog(_settings, ApplySettings)) RegisterHotkeys(_settings);
    }

    private string? ApplySettings(AppSettings edited)
    {
        var failed = RegisterHotkeys(edited);
        if (failed.Count > 0)
        {
            _hotkeys.UnregisterAll();
            return $"Комбинация {string.Join(", ", failed)} уже занята другой программой — выберите другую.";
        }

        edited.OverlayVisible = _main.IsOverlayOn;
        _settings = edited;
        _store.Save(_settings);
        _main.HotkeyWarning = null;
        _overlay?.SetCorner(_settings.OverlayCorner);
        UpdateHotkeyLabels();
        return null;
    }
}
