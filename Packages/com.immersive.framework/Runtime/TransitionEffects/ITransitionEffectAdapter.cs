using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Adapter-facing contract for executing one Transition Effect request.
    /// Implementations may perform concrete Unity operations, but they must return explicit results and must not
    /// own Transition orchestration, Gate admission, SceneLifecycle, RouteLifecycle, ActivityFlow or Pause.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19D Transition Effect adapter contract; no discovery registry or lifecycle ownership.")]
    public interface ITransitionEffectAdapter
    {
        /// <summary>Human-readable adapter name for diagnostics.</summary>
        string AdapterName { get; }

        /// <summary>Returns true when this adapter can execute the requested effect kind.</summary>
        bool Supports(TransitionEffectKind effectKind);

        /// <summary>
        /// Executes a single effect request and returns an explicit result.
        /// Required failures must block through TransitionEffectResult.BlocksTransition; optional failures remain non-blocking by requiredness.
        /// </summary>
        TransitionEffectResult Execute(TransitionEffectRequest request);
    }
}
