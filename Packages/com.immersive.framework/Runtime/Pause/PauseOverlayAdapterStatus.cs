using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Result status for a Pause overlay adapter action.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23C Pause Overlay adapter status boundary; no UI implementation.")]
    public enum PauseOverlayAdapterStatus
    {
        Unknown = 0,
        Succeeded = 10,
        Skipped = 20,
        Failed = 30,
        Rejected = 40
    }
}
