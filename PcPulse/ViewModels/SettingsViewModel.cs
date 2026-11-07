using System.Windows.Input;
using PcPulse.Core.Settings;

namespace PcPulse.ViewModels;

/// <summary>Edits a copy of the settings; nothing changes until Save succeeds.</summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _draft;
    private readonly Func<AppSettings, string?> _apply;
    private Hotkey _overlayHotkey;
    private Hotkey _windowHotkey;
    private OverlayCorner _corner;
    private bool _minimizeToTray;
    private bool _closeToTray;
    private bool _startInTray;
    private string? _error;

    /// <param name="apply">Applies the edited settings; returns an error to show, or null on success.</param>
    public SettingsViewModel(AppSettings current, Func<AppSettings, string?> apply)
    {
        _draft = current.Clone();
        _apply = apply;
        _overlayHotkey = _draft.OverlayHotkey;
        _windowHotkey = _draft.WindowHotkey;
        _corner = _draft.OverlayCorner;
        _minimizeToTray = _draft.MinimizeToTray;
        _closeToTray = _draft.CloseToTray;
        _startInTray = _draft.StartInTray;
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(false));
    }

    /// <summary>The dialog should close; true when settings were saved.</summary>
    public event Action<bool>? CloseRequested;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public Hotkey OverlayHotkey
    {
        get => _overlayHotkey;
        set { if (Set(ref _overlayHotkey, value)) Error = null; }
    }

    public Hotkey WindowHotkey
    {
        get => _windowHotkey;
        set { if (Set(ref _windowHotkey, value)) Error = null; }
    }

    public OverlayCorner Corner { get => _corner; set => Set(ref _corner, value); }
    public bool MinimizeToTray { get => _minimizeToTray; set => Set(ref _minimizeToTray, value); }
    public bool CloseToTray { get => _closeToTray; set => Set(ref _closeToTray, value); }
    public bool StartInTray { get => _startInTray; set => Set(ref _startInTray, value); }

    public string? Error
    {
        get => _error;
        private set
        {
            if (Set(ref _error, value)) OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => _error != null;

    private void Save()
    {
        if (!_overlayHotkey.IsEmpty && _overlayHotkey == _windowHotkey)
        {
            Error = "Одна и та же комбинация назначена на два действия.";
            return;
        }

        _draft.OverlayHotkey = _overlayHotkey;
        _draft.WindowHotkey = _windowHotkey;
        _draft.OverlayCorner = _corner;
        _draft.MinimizeToTray = _minimizeToTray;
        _draft.CloseToTray = _closeToTray;
        _draft.StartInTray = _startInTray;

        Error = _apply(_draft.Clone());
        if (Error == null) CloseRequested?.Invoke(true);
    }
}
