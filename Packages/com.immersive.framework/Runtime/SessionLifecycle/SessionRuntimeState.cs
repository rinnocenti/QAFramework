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
            SessionContentSet sessionContentSet,
            SceneLifecycleLoadResult primarySceneResult,
            ActivityFlowStartResult activityFlowResult,
            bool sessionStarted)
        {
            GameApplication = gameApplication;
            CurrentRoute = currentRoute;
            RouteLifecycleResult = routeLifecycleResult;
            RouteContentSet = routeContentSet;
            SessionContentSet = sessionContentSet;
            PrimarySceneResult = primarySceneResult;
            ActivityFlowResult = activityFlowResult;
            SessionStarted = sessionStarted;
        }

        public GameApplicationAsset GameApplication { get; }

        public RouteAsset CurrentRoute { get; }

        public RouteLifecycleStartResult RouteLifecycleResult { get; }

        public RouteRuntimeState RouteState => RouteLifecycleResult.RouteState;

        public RouteContentSet RouteContentSet { get; }

        public SessionContentSet SessionContentSet { get; }

        public SceneLifecycleLoadResult PrimarySceneResult { get; }

        public ActivityFlowStartResult ActivityFlowResult { get; }

        public ActivityAsset CurrentActivity => ActivityFlowResult.Activity;

        public string CurrentRouteName => CurrentRoute != null ? CurrentRoute.RouteName : string.Empty;

        public string CurrentRouteIdentity => RouteState.DiagnosticIdentity;

        public string CurrentActivityName => CurrentActivity != null ? CurrentActivity.ActivityName : string.Empty;

        public string ActiveSceneName => PrimarySceneResult.SceneName;

        public string ActiveScenePath => PrimarySceneResult.ScenePath;

        public bool SessionStarted { get; }

        public bool HasActiveRoute => CurrentRoute != null;

        public bool HasRouteState => RouteState.HasRoute;

        public bool HasActiveActivity => CurrentActivity != null;

        public bool HasSessionContent => SessionContentSet.HasContent;

        public int SessionContentCount => SessionContentSet.Count;

        public static SessionRuntimeState Empty(GameApplicationAsset gameApplication)
        {
            return new SessionRuntimeState(gameApplication, null, default, default, SessionContentSet.Empty(), default, default, false);
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
                SessionContentSet.Empty(),
                gameFlowResult.SceneLifecycleResult,
                gameFlowResult.RouteLifecycleResult.ActivityFlowResult,
                gameFlowResult.Started);
        }

        public static SessionRuntimeState FromRouteRequestResult(
            SessionRuntimeState previousState,
            FrameworkRouteRequestResult routeRequestResult,
            bool sessionStarted)
        {
            return new SessionRuntimeState(
                previousState.GameApplication,
                routeRequestResult.TargetRoute,
                routeRequestResult.RouteLifecycleResult,
                routeRequestResult.RouteLifecycleResult.RouteContentSet,
                previousState.SessionContentSet,
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
                previousState.SessionContentSet,
                previousState.PrimarySceneResult,
                activityRequestResult.ActivityFlowResult,
                previousState.SessionStarted);
        }
    }
}
