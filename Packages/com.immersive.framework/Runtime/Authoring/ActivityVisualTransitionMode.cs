using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Activity-level policy for using the session TransitionSurface during Activity requests.
    /// This selects whether Activity Flow asks the Session UIGlobal transition capability to run; it does not own the surface or load scenes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24F Activity visual transition policy; Session UIGlobal remains the surface owner.")]
    public enum ActivityVisualTransitionMode
    {
        /// <summary>Activity switch/clear runs without a visual transition.</summary>
        Seamless = 0,

        /// <summary>Activity switch/clear runs the session TransitionSurface fade before and after the Activity operation.</summary>
        Fade = 10,

        /// <summary>Reserved for Activity content/scene loading. Until Activity loading exists, this behaves as Fade with no LoadingSurface.</summary>
        FadeWithLoading = 20
    }
}
