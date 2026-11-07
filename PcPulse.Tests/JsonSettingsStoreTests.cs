using PcPulse.Core.Settings;
using PcPulse.Infrastructure.Storage;

namespace PcPulse.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "PcPulse.Tests." + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "settings.json");

    [Fact]
    public void Reads_files_written_by_earlier_versions()
    {
        // Exactly what the pre-refactor app saved (WPF ModifierKeys/Key enums by name).
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, """
            {
              "OverlayHotkey": { "Modifiers": "Alt, Control", "Key": "O" },
              "WindowHotkey": { "Modifiers": "Alt, Control", "Key": "P" },
              "OverlayCorner": "TopLeft",
              "OverlayVisible": true,
              "MinimizeToTray": true,
              "CloseToTray": true,
              "StartInTray": false
            }
            """);

        var s = new JsonSettingsStore(FilePath).Load();

        Assert.Equal(new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, "O"), s.OverlayHotkey);
        Assert.Equal("Ctrl + Alt + P", s.WindowHotkey.ToString());
        Assert.True(s.OverlayVisible);
        Assert.True(s.CloseToTray);
        Assert.False(s.StartInTray);
    }

    [Fact]
    public void Round_trips_every_setting()
    {
        var store = new JsonSettingsStore(FilePath);
        var saved = new AppSettings
        {
            OverlayHotkey = new Hotkey(HotkeyModifiers.Shift, "F9"),
            WindowHotkey = Hotkey.None,
            OverlayCorner = OverlayCorner.BottomRight,
            OverlayVisible = true,
            MinimizeToTray = false,
            CloseToTray = true,
            StartInTray = true,
        };

        store.Save(saved);
        var loaded = store.Load();

        Assert.Equal(saved.OverlayHotkey, loaded.OverlayHotkey);
        Assert.True(loaded.WindowHotkey.IsEmpty);
        Assert.Equal(OverlayCorner.BottomRight, loaded.OverlayCorner);
        Assert.True(loaded.OverlayVisible);
        Assert.False(loaded.MinimizeToTray);
        Assert.True(loaded.CloseToTray);
        Assert.True(loaded.StartInTray);
        Assert.DoesNotContain("IsEmpty", File.ReadAllText(FilePath));
    }

    [Fact]
    public void Missing_or_broken_file_gives_defaults()
    {
        Assert.Equal("Ctrl + Shift + O", new JsonSettingsStore(FilePath).Load().OverlayHotkey.ToString());

        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, "{ not json");
        Assert.Equal("Ctrl + Shift + P", new JsonSettingsStore(FilePath).Load().WindowHotkey.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }
}
