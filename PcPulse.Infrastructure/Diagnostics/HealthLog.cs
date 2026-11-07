using System.Diagnostics;
using System.Globalization;

namespace PcPulse.Infrastructure.Diagnostics;

/// <summary>
/// Appends this process's memory, handles and CPU use to a log a minute after start and then every interval,
/// so a multi-day run can be checked for leaks. Call <see cref="OnTick"/> often (e.g. every poll); it only writes when due.
/// "heap" is the live managed heap after a full collection, so unlike raw GC numbers it doesn't include
/// garbage waiting to be collected: a steady climb there is a real leak.
/// The file is trimmed to its newer half past 512 KB (about 3 500 lines, months of hourly entries).
/// </summary>
public sealed class HealthLog
{
    private const long MaxBytes = 512 * 1024;

    private readonly string _path;
    private readonly string _fallbackPath;
    private readonly TimeSpan _interval;
    private readonly TimeProvider _time;
    private readonly DateTimeOffset _started;
    private DateTimeOffset _next;
    private DateTimeOffset _lastAt;
    private TimeSpan _lastCpu;

    public HealthLog(string path, TimeSpan interval, TimeProvider time)
    {
        _path = path;
        _fallbackPath = Path.Combine(Path.GetTempPath(), "PcPulse-health-error.txt");
        _interval = interval;
        _time = time;
        _started = _lastAt = time.GetLocalNow();
        _next = _started.AddMinutes(1);
        using var self = Process.GetCurrentProcess();
        _lastCpu = self.TotalProcessorTime;
    }

    /// <summary>Hourly, or every PCPULSE_HEALTH_MINUTES minutes when that variable is set (for testing).</summary>
    public static TimeSpan IntervalFromEnvironment() =>
        int.TryParse(Environment.GetEnvironmentVariable("PCPULSE_HEALTH_MINUTES"), out int minutes) && minutes > 0
            ? TimeSpan.FromMinutes(minutes)
            : TimeSpan.FromHours(1);

    public void OnTick()
    {
        var now = _time.GetLocalNow();
        if (now < _next) return;
        _next = now + _interval;
        Append(now, Measure(now));
    }

    private string Measure(DateTimeOffset now)
    {
        try
        {
            long liveHeap = GC.GetTotalMemory(forceFullCollection: true);
            using var self = Process.GetCurrentProcess();
            var cpu = self.TotalProcessorTime;
            double wallSec = (now - _lastAt).TotalSeconds;
            double cpuPct = wallSec > 0 ? 100 * (cpu - _lastCpu).TotalSeconds / wallSec / Environment.ProcessorCount : 0;
            _lastCpu = cpu;
            _lastAt = now;

            return string.Format(CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm} | pid {1,6} | uptime {2,6:0.0} h | private {3,6:0.0} MB | ws {4,6:0.0} MB | heap {5,5:0.0} MB | handles {6,5} | threads {7,3} | cpu {8:0.00} %",
                now, Environment.ProcessId, (now - _started).TotalHours, self.PrivateMemorySize64 / 1048576.0,
                self.WorkingSet64 / 1048576.0, liveHeap / 1048576.0, self.HandleCount, self.Threads.Count, cpuPct);
        }
        catch (Exception ex)
        {
            return $"{now:yyyy-MM-dd HH:mm} | pid {Environment.ProcessId} | ERROR reading stats: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private void Append(DateTimeOffset now, string line)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            TrimIfLarge();
            File.AppendAllText(_path, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Never let diagnostics take the monitor down, but leave a trace of why the log is silent.
            try { File.AppendAllText(_fallbackPath, $"{now:u} {_path}: {ex}{Environment.NewLine}"); }
            catch { }
        }
    }

    private void TrimIfLarge()
    {
        var info = new FileInfo(_path);
        if (!info.Exists || info.Length < MaxBytes) return;
        var lines = File.ReadAllLines(_path);
        File.WriteAllLines(_path, lines.Skip(lines.Length / 2));
    }
}
