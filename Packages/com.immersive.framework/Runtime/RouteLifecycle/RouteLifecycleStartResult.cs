using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Minimal immutable result for starting a Route.
    /// This is diagnostics data for route lifecycle ownership, not a global route service.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct RouteLifecycleStartResult
    {
        public RouteLifecycleStartResult(
            bool started,
            string message,
            RouteAsset route,
            RouteAsset previousRoute,
            RouteExitResult routeExitResult,
            RouteRuntimeState routeState,
            SceneLifecycleLoadResult sceneLifecycleResult,
            RouteSceneCompositionResult routeSceneCompositionResult,
            RouteContentSet routeContentSet,
            RouteContentLifecycleDispatchResult routeContentEnterResult,
            RouteContentLifecycleDispatchResult routeContentExitResult,
            ActivityFlowStartResult activityFlowResult)
        {
            Started = started;
            Message = message ?? string.Empty;
            Route = route;
            PreviousRoute = previousRoute;
            RouteExitResult = routeExitResult;
            RouteState = routeState;
            SceneLifecycleResult = sceneLifecycleResult;
            RouteSceneCompositionResult = routeSceneCompositionResult;
            RouteContentSet = routeContentSet;
            RouteContentEnterResult = routeContentEnterResult;
            RouteContentExitResult = routeContentExitResult;
            ActivityFlowResult = activityFlowResult;
        }

        public bool Started { get; }

        public string Message { get; }

        public RouteAsset Route { get; }

        public RouteAsset PreviousRoute { get; }

        public bool ReplacedPreviousRoute => PreviousRoute != null;

        public RouteExitResult RouteExitResult { get; }

        public bool HasRouteExitResult => RouteExitResult.HasRoute;

        public RouteRuntimeState RouteState { get; }

        public SceneLifecycleLoadResult SceneLifecycleResult { get; }

        public RouteSceneCompositionResult RouteSceneCompositionResult { get; }

        public RouteContentSet RouteContentSet { get; }

        public RouteContentLifecycleDispatchResult RouteContentEnterResult { get; }

        public RouteContentLifecycleDispatchResult RouteContentExitResult { get; }

        public ActivityFlowStartResult ActivityFlowResult { get; }

        public static RouteLifecycleStartResult Failed(string message)
        {
            return new RouteLifecycleStartResult(false, message, null, null, default, default, default, default, default, default, default, default);
        }

        public static RouteLifecycleStartResult StartedWith(
            RouteAsset route,
            RouteRuntimeState previousRouteState,
            SceneLifecycleLoadResult sceneLifecycleResult,
            RouteSceneCompositionResult routeSceneCompositionResult,
            RouteContentSet routeContentSet,
            RouteContentLifecycleDispatchResult routeContentEnterResult,
            RouteContentLifecycleDispatchResult routeContentExitResult,
            ActivityFlowStartResult activityFlowResult,
            string source,
            string reason)
        {
            var previousRoute = previousRouteState.Route;
            var routeExitResult = RouteExitResult.Exited(previousRouteState, route, source, reason);
            var routeState = RouteRuntimeState.EnteredWith(
                route,
                sceneLifecycleResult,
                routeSceneCompositionResult,
                routeContentSet,
                activityFlowResult,
                source,
                reason);
            var routeStateMessage = routeState.HasIdentity ? $" routeIdentity='{routeState.DiagnosticIdentity}'." : string.Empty;
            var routeExitMessage = routeExitResult.HasRoute ? $" routeExit='{routeExitResult.DiagnosticStatus}'." : string.Empty;
            var routeContentExitMessage = routeContentExitResult.Executed
                ? $" routeContentExit='{routeContentExitResult.DiagnosticStatus}' routeContentExitBindings='{routeContentExitResult.BindingCount}' routeContentExitReceivers='{routeContentExitResult.ReceiverCount}' routeContentExitFailed='{routeContentExitResult.FailedReceiverCount}'."
                : string.Empty;
            var routeContentMessage = routeContentSet.HasContent ? $" {routeContentSet.DiagnosticMessage}" : string.Empty;
            var compositionMessage = !string.IsNullOrWhiteSpace(routeSceneCompositionResult.Message) ? $" {routeSceneCompositionResult.Message}" : string.Empty;
            var routeContentEnterMessage = routeContentEnterResult.Executed
                ? $" routeContentEnter='{routeContentEnterResult.DiagnosticStatus}' routeContentEnterBindings='{routeContentEnterResult.BindingCount}' routeContentEnterReceivers='{routeContentEnterResult.ReceiverCount}' routeContentEnterFailed='{routeContentEnterResult.FailedReceiverCount}'."
                : string.Empty;
            var activityMessage = !string.IsNullOrWhiteSpace(activityFlowResult.Message) ? $" {activityFlowResult.Message}" : string.Empty;
            var message = previousRoute != null
                ? $"Route Lifecycle switched from Route '{previousRoute.RouteName}' to Route '{route.RouteName}'.{routeStateMessage}{routeExitMessage}{routeContentExitMessage} {sceneLifecycleResult.Message}{compositionMessage}{routeContentMessage}{routeContentEnterMessage}{activityMessage}"
                : $"Route Lifecycle started Route '{route.RouteName}'.{routeStateMessage}{routeExitMessage}{routeContentExitMessage} {sceneLifecycleResult.Message}{compositionMessage}{routeContentMessage}{routeContentEnterMessage}{activityMessage}";

            return new RouteLifecycleStartResult(
                true,
                message,
                route,
                previousRoute,
                routeExitResult,
                routeState,
                sceneLifecycleResult,
                routeSceneCompositionResult,
                routeContentSet,
                routeContentEnterResult,
                routeContentExitResult,
                activityFlowResult);
        }
    }
}
