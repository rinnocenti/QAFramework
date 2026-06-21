using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.SessionLifecycle
{
    /// <summary>
    /// Explicit runtime state for the framework Session scope.
    /// This is state data owned by the Application Runtime host, not a service locator and not a content manager.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Session state boundary introduced by F2B; not game-facing API.")]
    internal readonly struct SessionRuntimeState
    {
        public SessionRuntimeState(
            GameApplicationAsset gameApplication,
            RouteAsset currentRoute,
            RouteLifecycleStartResult routeLifecycleResult,
            RouteContentSet routeContentSet,
            SceneLifecycleLoadResult primarySceneResult,
            ActivityFlowStartResult activityFlowResult,
            bool sessionStarted)
        {
            GameApplication = gameApplication;
            CurrentRoute = currentRoute;
            RouteLifecycleResult = routeLifecycleResult;
            RouteContentSet = routeContentSet;
            PrimarySceneResult = primarySceneResult;
            ActivityFlowResult = activityFlowResult;
            SessionStarted = sessionStarted;
        }

        public GameApplicationAsset GameApplication { get; }

        public RouteAsset CurrentRoute { get; }

        public RouteLifecycleStartResult RouteLifecycleResult { get; }

        public RouteContentSet RouteContentSet { get; }

        public SceneLifecycleLoadResult PrimarySceneResult { get; }

        public ActivityFlowStartResult ActivityFlowResult { get; }

        public ActivityAsset CurrentActivity => ActivityFlowResult.Activity;

        public string CurrentRouteName => CurrentRoute != null ? CurrentRoute.RouteName : string.Empty;

        public string CurrentActivityName => CurrentActivity != null ? CurrentActivity.ActivityName : string.Empty;

        public string ActiveSceneName => PrimarySceneResult.SceneName;

        public string ActiveScenePath => PrimarySceneResult.ScenePath;

        public bool SessionStarted { get; }

        public bool HasActiveRoute => CurrentRoute != null;

        public bool HasActiveActivity => CurrentActivity != null;

        public static SessionRuntimeState Empty(GameApplicationAsset gameApplication)
        {
            return new SessionRuntimeState(gameApplication, null, default, default, default, default, false);
        }

        public static SessionRuntimeState FromGameFlowResult(
            GameApplicationAsset gameApplication,
            FrameworkGameFlowStartResult gameFlowResult)
        {
            return new SessionRuntimeState(
                gameApplication,
                gameFlowResult.StartupRoute,
                gameFlowResult.RouteLifecycleResult,
                gameFlowResult.RouteLifecycleResult.RouteContentSet,
                gameFlowResult.SceneLifecycleResult,
                gameFlowResult.RouteLifecycleResult.ActivityFlowResult,
                gameFlowResult.Started);
        }

        public static SessionRuntimeState FromRouteRequestResult(
            GameApplicationAsset gameApplication,
            FrameworkRouteRequestResult routeRequestResult,
            bool sessionStarted)
        {
            return new SessionRuntimeState(
                gameApplication,
                routeRequestResult.TargetRoute,
                routeRequestResult.RouteLifecycleResult,
                routeRequestResult.RouteLifecycleResult.RouteContentSet,
                routeRequestResult.RouteLifecycleResult.SceneLifecycleResult,
                routeRequestResult.RouteLifecycleResult.ActivityFlowResult,
                sessionStarted);
        }

        public static SessionRuntimeState FromActivityRequestResult(
            SessionRuntimeState previousState,
            FrameworkActivityRequestResult activityRequestResult)
        {
            return new SessionRuntimeState(
                previousState.GameApplication,
                previousState.CurrentRoute,
                previousState.RouteLifecycleResult,
                previousState.RouteContentSet,
                previousState.PrimarySceneResult,
                activityRequestResult.ActivityFlowResult,
                previousState.SessionStarted);
        }
    }
}
