namespace PcPulse.Core.Abstractions;

/// <summary>Administrator rights of the current process.</summary>
public interface IElevation
{
    bool IsElevated { get; }

    /// <summary>Starts an elevated copy of this program. False if the user declined the UAC prompt.</summary>
    bool TryRelaunchElevated(IReadOnlyList<string> args);
}
