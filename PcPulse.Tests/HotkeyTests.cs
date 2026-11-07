using PcPulse.Core.Settings;

namespace PcPulse.Tests;

public class HotkeyTests
{
    [Theory]
    [InlineData(HotkeyModifiers.Control | HotkeyModifiers.Alt, "P", "Ctrl + Alt + P")]
    [InlineData(HotkeyModifiers.Shift | HotkeyModifiers.Control, "D1", "Ctrl + Shift + 1")]
    [InlineData(HotkeyModifiers.Windows, "NumPad5", "Win + Num 5")]
    [InlineData(HotkeyModifiers.None, "F10", "F10")]
    [InlineData(HotkeyModifiers.Alt, "Return", "Alt + Enter")]
    public void Formats_for_display(HotkeyModifiers modifiers, string key, string expected) =>
        Assert.Equal(expected, new Hotkey(modifiers, key).ToString());

    [Fact]
    public void None_is_empty_and_says_so()
    {
        Assert.True(Hotkey.None.IsEmpty);
        Assert.True(new Hotkey(HotkeyModifiers.Control, "").IsEmpty);
        Assert.Equal("не задано", Hotkey.None.ToString());
    }

    [Fact]
    public void Equal_combinations_compare_equal() =>
        Assert.Equal(new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, "O"),
                     new Hotkey(HotkeyModifiers.Alt | HotkeyModifiers.Control, "O"));
}
