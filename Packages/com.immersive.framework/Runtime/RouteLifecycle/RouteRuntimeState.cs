using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Explicit typed snapshot for the active Route scope.
    /// This is state data owned by Route Lifecycle, not a service locator, manager, loader or release executor.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Route state boundary introduced by F3B; not game-facing API.")]
    internal readonly struct RouteRuntimeState
    {
        public RouteRuntimeState(
            RouteAsset route,
            FrameworkIdentityKey routeIdentity,
            SceneLifecycleLoadResult sceneLifecycleResult,
            RouteContentSet routeContentSet,
            ActivityFlowStartResult activityFlowResult,
            string source,
            string reason,
            bool entered)
        {
            Route = route;
            RouteIdentity = routeIdentity;
            SceneLifecycleResult = sceneLifecycleResult;
            RouteContentSet = routeContentSet;
            ActivityFlowResult = activityFlowResult;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Entered = entered;
        }

        public RouteAsset Route { get; }

        public FrameworkIdentityKey RouteIdentity { get; }

        public SceneLifecycleLoadResult SceneLifecycleResult { get; }

        public RouteContentSet RouteContentSet { get; }

        public ActivityFlowStartResult ActivityFlowResult { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool Entered { get; }

        public ActivityRuntimeState ActivityState => ActivityFlowResult.ActivityState;

        public ActivityAsset CurrentActivity => ActivityState.Activity;

        public bool HasRoute => Route != null;

        public bool HasIdentity => RouteIdentity.IsValid;

        public bool HasRouteContent => RouteContentSet.HasContent;

        public bool HasActiveActivity => ActivityState.IsActive;

        public bool HasActivityIdentity => ActivityState.HasIdentity;

        public string RouteName => Route != null ? Route.RouteName : string.Empty;

        public string CurrentActivityName => ActivityState.ActivityName;

        public string CurrentActivityIdentity => ActivityState.DiagnosticIdentity;

        public string SceneName => SceneLifecycleResult.SceneName;

        public string ScenePath => SceneLifecycleResult.ScenePath;

        public string DiagnosticIdentity => HasIdentity ? RouteIdentity.StableText : string.Empty;

        public static RouteRuntimeState Empty()
        {
            return default;
        }

        public static RouteRuntimeState EnteredWith(
            RouteAsset route,
            SceneLifecycleLoadResult sceneLifecycleResult,
            RouteContentSet routeContentSet,
            ActivityFlowStartResult activityFlowResult,
            string source,
            string reason)
        {
            return new RouteRuntimeState(
                route,
                CreateRouteIdentity(route),
                sceneLifecycleResult,
                routeContentSet,
                activityFlowResult,
                source,
                reason,
                true);
        }

        private static FrameworkIdentityKey CreateRouteIdentity(RouteAsset route)
        {
            if (route == null)
            {
                throw new System.ArgumentNullException(nameof(route));
            }

            if (string.IsNullOrWhiteSpace(route.PrimaryScenePath))
            {
                throw new System.ArgumentException("Route identity requires a declared Primary Scene path.", nameof(route));
            }

            return FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, route.PrimaryScenePath);
        }
    }
}
