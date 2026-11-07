using PcPulse.Core.Models;

namespace PcPulse.Core.Abstractions;

/// <summary>Reads CPU, GPU and memory sensors. Implementations need not be thread-safe: one thread calls <see cref="Read"/>.</summary>
public interface IHardwareSensor : IDisposable
{
    HardwareSnapshot Read();
}
