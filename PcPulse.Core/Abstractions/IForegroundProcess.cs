namespace PcPulse.Core.Abstractions;

/// <summary>Which process owns the window the user is looking at.</summary>
public interface IForegroundProcess
{
    int ProcessId { get; }
}
