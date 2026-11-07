using System.Windows.Media;

namespace PcPulse.Presentation;

/// <summary>Accent colors and the green-to-red scales for temperatures and frame rates.</summary>
public static class Palette
{
    public static readonly Color Cpu = Color.FromRgb(0x4D, 0xC9, 0xFF);
    public static readonly Color Gpu = Color.FromRgb(0x76, 0xE0, 0x6A);
    public static readonly Color Fps = Color.FromRgb(0xB4, 0x7C, 0xFF);
    public static readonly Color Muted = Color.FromRgb(0x4A, 0x55, 0x70);

    public static readonly Color Good = Color.FromRgb(0x3D, 0xDC, 0x97);
    public static readonly Color Warm = Color.FromRgb(0xFF, 0xC8, 0x57);
    public static readonly Color Hot = Color.FromRgb(0xFF, 0x8C, 0x42);
    public static readonly Color Danger = Color.FromRgb(0xFF, 0x4D, 0x6D);

    public static Color ForTemp(double? celsius) => celsius switch
    {
        null => Muted,
        < 60 => Good,
        < 75 => Warm,
        < 85 => Hot,
        _ => Danger,
    };

    public static Color ForFps(double? fps) => fps switch
    {
        null => Muted,
        >= 60 => Good,
        >= 30 => Warm,
        _ => Danger,
    };
}
