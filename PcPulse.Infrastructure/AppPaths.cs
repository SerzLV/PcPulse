namespace PcPulse.Infrastructure;

/// <summary>Where PC Pulse keeps its files: %APPDATA%\PC Pulse.</summary>
public static class AppPaths
{
    public static readonly string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PC Pulse");

    public static readonly string SettingsFile = Path.Combine(DataFolder, "settings.json");

    public static readonly string HealthLogFile = Path.Combine(DataFolder, "health.log");
}
