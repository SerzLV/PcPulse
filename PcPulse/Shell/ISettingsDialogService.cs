using PcPulse.Core.Settings;

namespace PcPulse.Shell;

/// <summary>Shows the settings dialog.</summary>
public interface ISettingsDialogService
{
    bool IsOpen { get; }

    /// <summary>Brings an already open dialog to the front.</summary>
    void Activate();

    /// <summary>Shows the dialog modally on an editable copy of <paramref name="current"/>.</summary>
    /// <param name="apply">Applies the edited copy; returns an error to show, or null on success.</param>
    /// <returns>True when the user saved.</returns>
    bool ShowDialog(AppSettings current, Func<AppSettings, string?> apply);
}
