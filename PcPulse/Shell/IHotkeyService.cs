using PcPulse.Core.Settings;

namespace PcPulse.Shell;

/// <summary>System-wide hotkeys. Callbacks run on the UI thread.</summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>False when another program already owns the combination. An empty hotkey is a no-op success.</summary>
    bool Register(Hotkey hotkey, Action onPressed);

    void UnregisterAll();
}
