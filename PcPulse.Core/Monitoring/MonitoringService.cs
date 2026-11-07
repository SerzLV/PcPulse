using PcPulse.Core.Abstractions;
using PcPulse.Core.Models;

namespace PcPulse.Core.Monitoring;

/// <summary>Polls hardware sensors and the frame-rate monitor on a dedicated background thread.</summary>
public sealed class MonitoringService : IMonitoringService
{
    private readonly Func<IHardwareSensor> _createSensor;
    private readonly IFrameRateMonitor _frameRate;
    private readonly IForegroundProcess _foreground;
    private readonly TimeSpan _interval;
    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;

    /// <param name="createSensor">
    /// Called on the polling thread, because opening the sensors can take seconds and shouldn't block startup.
    /// </param>
    public MonitoringService(Func<IHardwareSensor> createSensor, IFrameRateMonitor frameRate,
                             IForegroundProcess foreground, TimeSpan interval)
    {
        _createSensor = createSensor;
        _frameRate = frameRate;
        _foreground = foreground;
        _interval = interval;
    }

    public event Action<MonitorSample>? SampleReady;
    public event Action<string>? Failed;
    public event Action? Tick;

    public void Start()
    {
        _frameRate.Start();
        _thread = new Thread(Loop) { IsBackground = true, Name = "Sensors" };
        _thread.Start();
    }

    private void Loop()
    {
        IHardwareSensor sensor;
        try { sensor = _createSensor(); }
        catch (Exception ex)
        {
            Failed?.Invoke(ex.Message);
            return;
        }

        using (sensor)
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    // Read before raising: `SampleReady?.Invoke(ReadSample(...))` would skip the read with no subscribers.
                    var sample = ReadSample(sensor);
                    SampleReady?.Invoke(sample);
                }
                catch (Exception ex)
                {
                    Failed?.Invoke(ex.Message);
                }
                Tick?.Invoke();
                _cts.Token.WaitHandle.WaitOne(_interval);
            }
        }
    }

    private MonitorSample ReadSample(IHardwareSensor sensor)
    {
        var hardware = sensor.Read();
        int foreground = _foreground.ProcessId;
        var fps = _frameRate.GetForTarget(foreground);
        return new MonitorSample(hardware, fps, fps?.ProcessId == foreground, _frameRate.Error);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _thread?.Join(TimeSpan.FromSeconds(2));
        _frameRate.Dispose();
    }
}
