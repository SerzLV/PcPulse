using System.Windows;
using PcPulse.Core.Settings;
using PcPulse.Shell;
using PcPulse.ViewModels;

namespace PcPulse.Views;

/// <summary>Shows <see cref="SettingsWindow"/> modally over the main window.</summary>
public sealed class SettingsDialogService : ISettingsDialogService
{
    private readonly Window _owner;
    private SettingsWindow? _open;

    public SettingsDialogService(Window owner) => _owner = owner;

    public bool IsOpen => _open != null;

    public void Activate() => _open?.Activate();

    public bool ShowDialog(AppSettings current, Func<AppSettings, string?> apply)
    {
        _open = new SettingsWindow(new SettingsViewModel(current, apply)) { Owner = _owner };
        try
        {
            return _open.ShowDialog() == true;
        }
        finally
        {
            _open = null;
        }
    }
}
