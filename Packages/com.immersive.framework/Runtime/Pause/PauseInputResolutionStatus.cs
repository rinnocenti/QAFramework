using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Result status for resolving a normalized Pause input signal.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23D Pause Input resolution status; passive contract only.")]
    public enum PauseInputResolutionStatus
    {
        Unknown = 0,
        ResolvedPauseRequest = 10,
        ResolvedMenuCommand = 20,
        Ignored = 30,
        RejectedUnsupported = 40,
        Failed = 50
    }
}
