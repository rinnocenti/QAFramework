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

        /// <summary>
        /// Reserved for future Activity content/scene loading. Until ActivityContentProfile and Activity scene composition exist, this behaves as Fade and keeps Loading skipped.
        /// </summary>
        FadeWithLoading = 20
    }
}
