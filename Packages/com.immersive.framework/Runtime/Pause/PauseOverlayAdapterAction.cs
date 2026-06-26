using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Adapter-facing action for Pause overlay presentation.
    /// This is a visual adapter action only; it does not request Pause, bind input, change Time.timeScale or own lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23C Pause Overlay adapter action boundary; no UI implementation.")]
    public enum PauseOverlayAdapterAction
    {
        Unknown = 0,
        Show = 10,
        Update = 20,
        Hide = 30
    }
}
