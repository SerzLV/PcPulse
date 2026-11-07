using System.Windows.Input;
using PcPulse.Core.Settings;

namespace PcPulse.Platform;

/// <summary>Converts between the core's <see cref="Hotkey"/> and WPF/Win32 key representations.</summary>
internal static class KeyMapping
{
    public static Hotkey ToHotkey(ModifierKeys modifiers, Key key) => new(ToHotkeyModifiers(modifiers), key.ToString());

    // ModifierKeys and HotkeyModifiers share values (Alt=1, Control=2, Shift=4, Windows=8).
    public static HotkeyModifiers ToHotkeyModifiers(ModifierKeys modifiers) => (HotkeyModifiers)(int)modifiers;

    public static Key ToWpfKey(Hotkey hotkey) => Enum.TryParse<Key>(hotkey.Key, out var key) ? key : Key.None;

    public static uint ToVirtualKey(Hotkey hotkey) => (uint)KeyInterop.VirtualKeyFromKey(ToWpfKey(hotkey));
}
