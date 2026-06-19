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
            bool gameFlowStarted)
        {
            GameApplication = gameApplication;
            CurrentRoute = currentRoute;
            RouteLifecycleResult = routeLifecycleResult;
            PrimarySceneResult = primarySceneResult;
            GameFlowStarted = gameFlowStarted;
        }

        public GameApplicationAsset GameApplication { get; }

        public RouteAsset CurrentRoute { get; }

        public RouteLifecycleStartResult RouteLifecycleResult { get; }

        public SceneLifecycleLoadResult PrimarySceneResult { get; }

        public bool GameFlowStarted { get; }

        public string CurrentRouteName => CurrentRoute != null ? CurrentRoute.RouteName : string.Empty;

        public string ActiveSceneName => PrimarySceneResult.SceneName;

        public string ActiveScenePath => PrimarySceneResult.ScenePath;

        public static FrameworkRuntimeState Empty(GameApplicationAsset gameApplication)
        {
            return new FrameworkRuntimeState(gameApplication, null, default, default, false);
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
                gameFlowStarted);
        }
    }
}
