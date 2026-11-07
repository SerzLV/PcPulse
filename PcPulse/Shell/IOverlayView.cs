using PcPulse.Core.Settings;

namespace PcPulse.Shell;

/// <summary>The always-on-top overlay shown over games.</summary>
public interface IOverlayView
{
    void Show();
    void Hide();
    void Close();
    void SetCorner(OverlayCorner corner);
}
