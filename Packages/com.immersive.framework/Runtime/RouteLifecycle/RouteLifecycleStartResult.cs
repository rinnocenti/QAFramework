using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Minimal immutable result for starting a Route.
    /// This is diagnostics data for route lifecycle ownership, not a global route service.
    /// </summary>
    internal readonly struct RouteLifecycleStartResult
    {
        public RouteLifecycleStartResult(
            bool started,
            string message,
            RouteAsset route,
            RouteAsset previousRoute,
            SceneLifecycleLoadResult sceneLifecycleResult)
        {
            Started = started;
            Message = message ?? string.Empty;
            Route = route;
            PreviousRoute = previousRoute;
            SceneLifecycleResult = sceneLifecycleResult;
        }

        public bool Started { get; }

        public string Message { get; }

        public RouteAsset Route { get; }

        public RouteAsset PreviousRoute { get; }

        public bool ReplacedPreviousRoute => PreviousRoute != null;

        public SceneLifecycleLoadResult SceneLifecycleResult { get; }

        public static RouteLifecycleStartResult Failed(string message)
        {
            return new RouteLifecycleStartResult(false, message, null, null, default);
        }

        public static RouteLifecycleStartResult StartedWith(
            RouteAsset route,
            RouteAsset previousRoute,
            SceneLifecycleLoadResult sceneLifecycleResult)
        {
            var message = previousRoute != null
                ? $"Route Lifecycle switched from Route '{previousRoute.RouteName}' to Route '{route.RouteName}'. {sceneLifecycleResult.Message}"
                : $"Route Lifecycle started Route '{route.RouteName}'. {sceneLifecycleResult.Message}";

            return new RouteLifecycleStartResult(
                true,
                message,
                route,
                previousRoute,
                sceneLifecycleResult);
        }
    }
}
