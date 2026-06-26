using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Pause-facing reason for consuming a Content Anchor.
    /// Purpose is diagnostic/contract intent only; it does not imply UI technology, prefab shape or input binding.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23B Pause Content Anchor consumer purpose vocabulary; no UI implementation.")]
    public enum PauseContentAnchorPurpose
    {
        /// <summary>Invalid default value. Pause Content Anchor requests must always use an explicit purpose.</summary>
        Unknown = 0,

        /// <summary>Pause needs an overlay root supplied by an existing Content Anchor declaration.</summary>
        OverlayRoot = 10,

        /// <summary>Pause needs a slot where pause content can be bound by a future adapter.</summary>
        ContentSlot = 20,

        /// <summary>Pause needs a diagnostic/reference point; this is not visual materialization.</summary>
        DiagnosticPoint = 30
    }
}
