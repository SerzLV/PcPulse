using System.ComponentModel;

namespace PcPulse.Shell;

/// <summary>What the shell needs from the main window. A WPF Window already provides most of it.</summary>
public interface IMainWindowView
{
    event CancelEventHandler Closing;
    event EventHandler Closed;

    /// <summary>The window was minimized, by its button or the taskbar.</summary>
    event EventHandler? Minimized;

    /// <summary>Visible, not minimized and actually the foreground window.</summary>
    bool IsInFront { get; }

    /// <summary>Shows the window (restoring it if minimized) and brings it to the front.</summary>
    void ShowAndActivate();

    void Hide();
    void Minimize();
    void Close();
}
