using PcPulse.Core.Models;

namespace PcPulse.Core.Abstractions;

/// <summary>Reports every Present call made by any process. <see cref="Presented"/> may fire on a background thread.</summary>
public interface IPresentEventSource : IDisposable
{
    event Action<PresentEvent>? Presented;

    /// <summary>Why the source isn't delivering events, or null when it works.</summary>
    string? Error { get; }

    void Start();
}
