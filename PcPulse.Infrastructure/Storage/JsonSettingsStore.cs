using System.Text.Json;
using System.Text.Json.Serialization;
using PcPulse.Core.Abstractions;
using PcPulse.Core.Settings;

namespace PcPulse.Infrastructure.Storage;

/// <summary>Settings as indented JSON with enums by name (e.g. "Modifiers": "Alt, Control").</summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    public JsonSettingsStore(string path) => _path = path;

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), Options) ?? new AppSettings();
        }
        catch
        {
            // A broken file just falls back to defaults.
        }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options));
        }
        catch
        {
            // Not being able to persist settings shouldn't take the monitor down.
        }
    }
}
