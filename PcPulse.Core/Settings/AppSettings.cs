namespace PcPulse.Core.Settings;

public enum OverlayCorner { TopLeft, TopRight, BottomLeft, BottomRight }

/// <summary>User preferences. Property names are the settings-file format, so renaming one loses saved values.</summary>
public sealed class AppSettings
{
    public Hotkey OverlayHotkey { get; set; } = new(HotkeyModifiers.Control | HotkeyModifiers.Shift, "O");
    public Hotkey WindowHotkey { get; set; } = new(HotkeyModifiers.Control | HotkeyModifiers.Shift, "P");
    public OverlayCorner OverlayCorner { get; set; } = OverlayCorner.TopLeft;
    public bool OverlayVisible { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; }
    public bool StartInTray { get; set; }

    public AppSettings Clone() => (AppSettings)MemberwiseClone();
}
