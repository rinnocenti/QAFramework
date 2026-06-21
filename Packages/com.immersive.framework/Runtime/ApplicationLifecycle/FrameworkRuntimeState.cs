using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.SessionLifecycle;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ApplicationLifecycle
{
    /// <summary>
    /// Minimal immutable snapshot of the current framework runtime state.
    /// This is diagnostics/state data owned by the Application Runtime, not a service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct FrameworkRuntimeState
    {
        public FrameworkRuntimeState(
            GameApplicationAsset gameApplication,
            RouteAsset currentRoute,
            RouteLifecycleStartResult routeLifecycleResult,
            RouteContentSet routeContentSet,
            SceneLifecycleLoadResult primarySceneResult,
            ActivityFlowStartResult activityFlowResult,
            bool gameFlowStarted)
        {
            SessionState = new SessionRuntimeState(
                gameApplication,
                currentRoute,
                routeLifecycleResult,
                routeContentSet,
                SessionContentSet.Empty(),
                primarySceneResult,
                activityFlowResult,
                gameFlowStarted);
        }

        public SessionRuntimeState SessionState { get; }

        public GameApplicationAsset GameApplication => SessionState.GameApplication;

        public RouteAsset CurrentRoute => SessionState.CurrentRoute;

        public RouteLifecycleStartResult RouteLifecycleResult => SessionState.RouteLifecycleResult;

        public RouteContentSet RouteContentSet => SessionState.RouteContentSet;

        public SessionContentSet SessionContentSet => SessionState.SessionContentSet;

        public SceneLifecycleLoadResult PrimarySceneResult => SessionState.PrimarySceneResult;

        public ActivityFlowStartResult ActivityFlowResult => SessionState.ActivityFlowResult;

        public ActivityAsset CurrentActivity => SessionState.CurrentActivity;

        public string CurrentActivityName => SessionState.CurrentActivityName;

        public bool GameFlowStarted => SessionState.SessionStarted;

        public string CurrentRouteName => SessionState.CurrentRouteName;

        public string ActiveSceneName => SessionState.ActiveSceneName;

        public string ActiveScenePath => SessionState.ActiveScenePath;

        public static FrameworkRuntimeState Empty(GameApplicationAsset gameApplication)
        {
            return new FrameworkRuntimeState(gameApplication, null, default, default, default, default, false);
        }

        public static FrameworkRuntimeState FromGameFlowResult(
            GameApplicationAsset gameApplication,
            FrameworkGameFlowStartResult gameFlowResult)
        {
            return new FrameworkRuntimeState(
                gameApplication,
                gameFlowResult.StartupRoute,
                gameFlowResult.RouteLifecycleResult,
                gameFlowResult.RouteLifecycleResult.RouteContentSet,
                gameFlowResult.SceneLifecycleResult,
                gameFlowResult.RouteLifecycleResult.ActivityFlowResult,
                gameFlowResult.Started);
        }

        public static FrameworkRuntimeState FromRouteRequestResult(
            FrameworkRuntimeState previousState,
            FrameworkRouteRequestResult routeRequestResult,
            bool gameFlowStarted)
        {
            return new FrameworkRuntimeState(
                SessionRuntimeState.FromRouteRequestResult(previousState.SessionState, routeRequestResult, gameFlowStarted));
        }

        private FrameworkRuntimeState(SessionRuntimeState sessionState)
        {
            SessionState = sessionState;
        }

        public static FrameworkRuntimeState FromActivityRequestResult(
            FrameworkRuntimeState previousState,
            FrameworkActivityRequestResult activityRequestResult)
        {
            return new FrameworkRuntimeState(
                SessionRuntimeState.FromActivityRequestResult(previousState.SessionState, activityRequestResult));
        }
    }
}
