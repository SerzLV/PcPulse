using PcPulse.Core.Models;

namespace PcPulse.Core.Abstractions;

/// <summary>Strategy for picking which process's frame rate to show.</summary>
public interface IFpsTargetSelector
{
    /// <param name="busiestFirst">Recently presenting processes, highest frame rate first.</param>
    FrameStats? Select(IReadOnlyList<FrameStats> busiestFirst, int foregroundProcessId);
}
