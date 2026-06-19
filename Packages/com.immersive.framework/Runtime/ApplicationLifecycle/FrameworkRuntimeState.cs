using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.ApplicationLifecycle
{
    /// <summary>
    /// Minimal immutable snapshot of the current framework runtime state.
    /// This is diagnostics/state data owned by the Application Runtime, not a service locator.
    /// </summary>
    internal readonly struct FrameworkRuntimeState
    {
        public FrameworkRuntimeState(
            GameApplicationAsset gameApplication,
            RouteAsset currentRoute,
            RouteLifecycleStartResult routeLifecycleResult,
            SceneLifecycleLoadResult primarySceneResult,
            ActivityFlowStartResult activityFlowResult,
            bool gameFlowStarted)
        {
            GameApplication = gameApplication;
            CurrentRoute = currentRoute;
            RouteLifecycleResult = routeLifecycleResult;
            PrimarySceneResult = primarySceneResult;
            ActivityFlowResult = activityFlowResult;
            GameFlowStarted = gameFlowStarted;
        }

        public GameApplicationAsset GameApplication { get; }

        public RouteAsset CurrentRoute { get; }

        public RouteLifecycleStartResult RouteLifecycleResult { get; }

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
            return new FrameworkRuntimeState(gameApplication, null, default, default, default, false);
        }

        public static FrameworkRuntimeState FromGameFlowResult(
            GameApplicationAsset gameApplication,
            FrameworkGameFlowStartResult gameFlowResult)
        {
            return new FrameworkRuntimeState(
                gameApplication,
                gameFlowResult.StartupRoute,
                gameFlowResult.RouteLifecycleResult,
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
                routeRequestResult.RouteLifecycleResult.SceneLifecycleResult,
                routeRequestResult.RouteLifecycleResult.ActivityFlowResult,
                gameFlowStarted);
        }
    }
}
