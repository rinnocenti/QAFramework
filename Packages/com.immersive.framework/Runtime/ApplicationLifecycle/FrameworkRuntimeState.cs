using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;
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
            GameApplication = gameApplication;
            CurrentRoute = currentRoute;
            RouteLifecycleResult = routeLifecycleResult;
            RouteContentSet = routeContentSet;
            PrimarySceneResult = primarySceneResult;
            ActivityFlowResult = activityFlowResult;
            GameFlowStarted = gameFlowStarted;
        }

        public GameApplicationAsset GameApplication { get; }

        public RouteAsset CurrentRoute { get; }

        public RouteLifecycleStartResult RouteLifecycleResult { get; }

        public RouteContentSet RouteContentSet { get; }

        public SceneLifecycleLoadResult PrimarySceneResult { get; }

        public ActivityFlowStartResult ActivityFlowResult { get; }

        public ActivityAsset CurrentActivity => ActivityFlowResult.Activity;

        public string CurrentActivityName => CurrentActivity != null ? CurrentActivity.ActivityName : string.Empty;

        public bool GameFlowStarted { get; }

        public string CurrentRouteName => CurrentRoute != null ? CurrentRoute.RouteName : string.Empty;

        public string ActiveSceneName => PrimarySceneResult.SceneName;

        public string ActiveScenePath => PrimarySceneResult.ScenePath;

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
            GameApplicationAsset gameApplication,
            FrameworkRouteRequestResult routeRequestResult,
            bool gameFlowStarted)
        {
            return new FrameworkRuntimeState(
                gameApplication,
                routeRequestResult.TargetRoute,
                routeRequestResult.RouteLifecycleResult,
                routeRequestResult.RouteLifecycleResult.RouteContentSet,
                routeRequestResult.RouteLifecycleResult.SceneLifecycleResult,
                routeRequestResult.RouteLifecycleResult.ActivityFlowResult,
                gameFlowStarted);
        }

        public static FrameworkRuntimeState FromActivityRequestResult(
            FrameworkRuntimeState previousState,
            FrameworkActivityRequestResult activityRequestResult)
        {
            return new FrameworkRuntimeState(
                previousState.GameApplication,
                previousState.CurrentRoute,
                previousState.RouteLifecycleResult,
                previousState.RouteContentSet,
                previousState.PrimarySceneResult,
                activityRequestResult.ActivityFlowResult,
                previousState.GameFlowStarted);
        }
    }
}
