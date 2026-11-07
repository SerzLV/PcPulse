using System.Text;
using PcPulse.Core.Fps;
using PcPulse.Infrastructure.Fps;
using PcPulse.Infrastructure.Hardware;
using PcPulse.Infrastructure.Windows;

namespace PcPulse.Infrastructure.Diagnostics;

/// <summary>Headless diagnostics (<c>PcPulse.exe --dump file</c>): samples sensors and FPS for a few seconds into a text file.</summary>
public static class SensorDump
{
    public static void Write(string path, bool isElevated)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"admin={isElevated}");
        try
        {
            using var fps = new FrameRateMonitor(new EtwPresentEventSource(), new ProcessNameResolver(),
                ForegroundFirstSelector.CreateDefault(), TimeProvider.System, Environment.ProcessId);
            fps.Start();
            sb.AppendLine($"fpsError={fps.Error ?? "none"}");

            using var sensor = new LibreHardwareSensor();
            for (int i = 0; i < 4; i++)
            {
                Thread.Sleep(1000);
                sb.AppendLine(sensor.Read().ToString());
                sb.AppendLine("fps: " + string.Join("; ", fps.GetAll().Take(6)
                    .Select(s => $"{s.ProcessName}({s.ProcessId}) {s.Fps:0.0} fps, 1% low {s.Low1Fps:0.0}")));
            }
            sb.AppendLine("--- sensors ---");
            foreach (var line in sensor.DescribeAll()) sb.AppendLine(line);
        }
        catch (Exception ex)
        {
            sb.AppendLine("ERROR: " + ex);
        }
        File.WriteAllText(path, sb.ToString());
    }
}
