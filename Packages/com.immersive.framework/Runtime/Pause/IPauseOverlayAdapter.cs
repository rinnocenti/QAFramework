using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Adapter-facing contract for presenting canonical Pause state on an overlay.
    /// Implementations may drive concrete UI later, but they must not request Pause, bind input, change Time.timeScale,
    /// create Content Anchors, execute Transition Effects, own Route/Activity lifecycle or become gameplay adapters.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23C Pause Overlay adapter boundary; no built-in UI/prefab implementation.")]
    public interface IPauseOverlayAdapter
    {
        /// <summary>Human-readable adapter name for diagnostics.</summary>
        string AdapterName { get; }

        /// <summary>Returns true when this adapter can present the supplied Pause overlay presentation.</summary>
        bool Supports(PauseOverlayPresentation presentation);

        /// <summary>Shows the Pause overlay for a canonical Pause presentation.</summary>
        PauseOverlayAdapterResult Show(PauseOverlayPresentation presentation);

        /// <summary>Updates the Pause overlay from canonical Pause presentation data.</summary>
        PauseOverlayAdapterResult Update(PauseOverlayPresentation presentation);

        /// <summary>Hides the Pause overlay for a canonical Pause presentation.</summary>
        PauseOverlayAdapterResult Hide(PauseOverlayPresentation presentation);
    }
}
