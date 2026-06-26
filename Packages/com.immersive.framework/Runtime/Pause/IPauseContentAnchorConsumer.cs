using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Port for code that consumes existing Content Anchor declarations for Pause content.
    /// Implementations prepare contract results only. They must not create anchors, instantiate prefabs, show UI,
    /// bind input, mutate Pause state, execute Transition Effects or own Route/Activity lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23B Pause Content Anchor consumer port; contract preparation only.")]
    public interface IPauseContentAnchorConsumer
    {
        PauseContentAnchorConsumerResult Prepare(
            PauseContentAnchorRequest request,
            ContentAnchorSet availableAnchors);
    }
}
