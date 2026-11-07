namespace PcPulse.Core.Abstractions;

/// <summary>Looks up a process's name by id.</summary>
public interface IProcessNameResolver
{
    /// <summary>The process name without ".exe", or a placeholder when the process is gone or inaccessible.</summary>
    string GetName(int processId);
}
