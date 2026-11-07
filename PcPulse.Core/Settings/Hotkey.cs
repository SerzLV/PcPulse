using System.Text.Json.Serialization;

namespace PcPulse.Core.Settings;

/// <summary>Modifier keys of a global hotkey. Values and names match WPF's ModifierKeys and Win32's MOD_* flags.</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8,
}

/// <summary>
/// A global key combination. <see cref="Key"/> is a WPF Key name (e.g. "P", "D1", "F10");
/// keeping it a string keeps the core free of UI types and matches the settings file format.
/// </summary>
public sealed record Hotkey(HotkeyModifiers Modifiers, string Key)
{
    public const string NoKey = "None";

    public static readonly Hotkey None = new(HotkeyModifiers.None, NoKey);

    [JsonIgnore]
    public bool IsEmpty => string.IsNullOrEmpty(Key) || Key == NoKey;

    /// <summary>"Ctrl + Alt + P", or "не задано" when empty.</summary>
    public override string ToString() =>
        IsEmpty ? "не задано" : string.Join(" + ", ModifierNames(Modifiers).Append(KeyDisplayName(Key)));

    public static IEnumerable<string> ModifierNames(HotkeyModifiers modifiers)
    {
        if (modifiers.HasFlag(HotkeyModifiers.Control)) yield return "Ctrl";
        if (modifiers.HasFlag(HotkeyModifiers.Alt)) yield return "Alt";
        if (modifiers.HasFlag(HotkeyModifiers.Shift)) yield return "Shift";
        if (modifiers.HasFlag(HotkeyModifiers.Windows)) yield return "Win";
    }

    private static string KeyDisplayName(string key) => key switch
    {
        ['D', var d] when char.IsAsciiDigit(d) => d.ToString(),
        _ when key.StartsWith("NumPad", StringComparison.Ordinal) => "Num " + key["NumPad".Length..],
        "OemTilde" => "`",
        "OemMinus" => "-",
        "OemPlus" => "=",
        "OemComma" => ",",
        "OemPeriod" => ".",
        "Return" => "Enter",
        "Next" => "PageDown",
        "Prior" => "PageUp",
        "Snapshot" => "PrintScreen",
        _ => key,
    };
}
