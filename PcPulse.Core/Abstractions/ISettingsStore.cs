using PcPulse.Core.Settings;

namespace PcPulse.Core.Abstractions;

/// <summary>Persists user settings.</summary>
public interface ISettingsStore
{
    /// <summary>The saved settings, or defaults when none are saved or the saved copy is unreadable.</summary>
    AppSettings Load();

    void Save(AppSettings settings);
}
