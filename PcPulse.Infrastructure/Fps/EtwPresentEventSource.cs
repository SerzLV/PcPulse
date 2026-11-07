using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using PcPulse.Core.Abstractions;
using PcPulse.Core.Models;

namespace PcPulse.Infrastructure.Fps;

/// <summary>
/// Present calls from ETW, the same events PresentMon uses: Microsoft-Windows-DXGI covers D3D10/11/12,
/// Microsoft-Windows-D3D9 covers D3D9. A real-time ETW session needs administrator rights.
/// </summary>
public sealed class EtwPresentEventSource : IPresentEventSource
{
    private static readonly Guid DxgiProvider = new("CA11C036-0102-4A2D-A6AD-F03CFED5D3C9");
    private static readonly Guid D3D9Provider = new("783ACA0A-790E-4D7F-8451-AA850511C6B9");
    private const int DxgiPresentStart = 42;
    private const int DxgiPresentMpoStart = 55;
    private const int D3D9PresentStart = 1;

    private const string SessionName = "PcPulse-FPS";

    private TraceEventSession? _session;

    public event Action<PresentEvent>? Presented;

    public string? Error { get; private set; }

    public void Start()
    {
        if (TraceEventSession.IsElevated() != true)
        {
            Error = "нужны права администратора";
            return;
        }

        try
        {
            // Creating a session with an existing name stops the stale one left by a crashed run.
            _session = new TraceEventSession(SessionName) { StopOnDispose = true };
            _session.EnableProvider(DxgiProvider, TraceEventLevel.Informational);
            _session.EnableProvider(D3D9Provider, TraceEventLevel.Informational);

            var source = _session.Source;
            new RegisteredTraceEventParser(source).All += OnEvent;
            source.UnhandledEvents += OnEvent;

            new Thread(() =>
            {
                try { source.Process(); }
                catch (Exception ex) { Error = ex.Message; }
            }) { IsBackground = true, Name = "ETW presents" }.Start();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            _session?.Dispose();
            _session = null;
        }
    }

    private void OnEvent(TraceEvent e)
    {
        int id = (int)e.ID;
        GraphicsApi api;
        if (e.ProviderGuid == DxgiProvider && id is DxgiPresentStart or DxgiPresentMpoStart) api = GraphicsApi.Dxgi;
        else if (e.ProviderGuid == D3D9Provider && id == D3D9PresentStart) api = GraphicsApi.D3D9;
        else return;

        Presented?.Invoke(new PresentEvent(e.ProcessID, api, e.TimeStampRelativeMSec));
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
    }
}
