using PcPulse.Core.Settings;

namespace PcPulse.Shell;

/// <summary>Notification-area icon and its menu.</summary>
public interface ITrayIcon : IDisposable
{
    event Action? OpenRequested;
    event Action? OverlayToggleRequested;
    event Action? SettingsRequested;
    event Action? ExitRequested;

    void SetOverlayState(bool isOn, Hotkey hotkey);
    void SetTooltip(string text);

    /// <summary>Tells the user once per run that the window went to the tray.</summary>
    void ShowHiddenHintOnce();
}
