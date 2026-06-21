using System;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Minimal owner for starting and switching Routes.
    /// It owns the active Route identity and delegates scene loading to Scene Lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class RouteLifecycleRuntime
    {
        private readonly SceneLifecycleRuntime _sceneLifecycleRuntime = new SceneLifecycleRuntime();
        private readonly ActivityFlowRuntime _activityFlowRuntime = new ActivityFlowRuntime();
        private readonly EventBus<RouteEnteredEvent> _routeEnteredEvents = new EventBus<RouteEnteredEvent>();
        private readonly EventBus<RouteExitedEvent> _routeExitedEvents = new EventBus<RouteExitedEvent>();
        private RouteAsset _currentRoute;
        private RouteContentSet _currentRouteContentSet;

        internal RouteAsset CurrentRoute => _currentRoute;

        internal RouteContentSet CurrentRouteContentSet => _currentRouteContentSet;

        internal ActivityAsset CurrentActivity => _activityFlowRuntime.CurrentActivity;

        internal bool HasActiveRoute => _currentRoute != null;

        internal bool HasActiveActivity => _activityFlowRuntime.HasActiveActivity;

        internal bool IsRouteActive(RouteAsset route)
        {
            return route != null && ReferenceEquals(_currentRoute, route);
        }

        internal IEventBinding SubscribeRouteEntered(Action<RouteEnteredEvent> handler)
        {
            return _routeEnteredEvents.Subscribe(handler);
        }

        internal IEventBinding SubscribeRouteExited(Action<RouteExitedEvent> handler)
        {
            return _routeExitedEvents.Subscribe(handler);
        }

        internal bool IsActivityActive(ActivityAsset activity)
        {
            return _activityFlowRuntime.IsActivityActive(activity);
        }

        internal async Task<RouteLifecycleStartResult> StartRouteAsync(
            RouteAsset route,
            string source,
            string reason)
        {
            if (route == null)
            {
                return RouteLifecycleStartResult.Failed("Route is missing.");
            }

            if (!route.HasPrimaryScene)
            {
                return RouteLifecycleStartResult.Failed("Route Primary Scene is missing.");
            }

            var previousRoute = _currentRoute;
            var routeContentPlan = RouteContentMaterializationPlan.FromRoute(route);
            var sceneLifecycleResult = await _sceneLifecycleRuntime.LoadPrimarySceneAsync(route);
            if (!sceneLifecycleResult.Loaded)
            {
                return RouteLifecycleStartResult.Failed(sceneLifecycleResult.Message);
            }

            var routeContentSet = RouteContentSet.FromPrimaryScene(
                route,
                routeContentPlan,
                sceneLifecycleResult,
                source,
                reason);

            var activityFlowResult = await _activityFlowRuntime.StartStartupActivityAsync(route, source, reason);
            if (!activityFlowResult.Completed)
            {
                return RouteLifecycleStartResult.Failed(activityFlowResult.Message);
            }

            _currentRoute = route;
            _currentRouteContentSet = routeContentSet;
            PublishRouteTransition(previousRoute, route, source, reason);
            return RouteLifecycleStartResult.StartedWith(route, previousRoute, sceneLifecycleResult, routeContentSet, activityFlowResult);
        }

        private void PublishRouteTransition(
            RouteAsset previousRoute,
            RouteAsset nextRoute,
            string source,
            string reason)
        {
            if (previousRoute != null && !ReferenceEquals(previousRoute, nextRoute))
            {
                _routeExitedEvents.Publish(new RouteExitedEvent(previousRoute, nextRoute, source, reason));
            }

            if (nextRoute != null && !ReferenceEquals(previousRoute, nextRoute))
            {
                _routeEnteredEvents.Publish(new RouteEnteredEvent(nextRoute, previousRoute, source, reason));
            }
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(
            ActivityAsset activity,
            string source,
            string reason)
        {
            if (_currentRoute == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("No active Route is available."));
            }

            return _activityFlowRuntime.StartActivityAsync(activity, source, reason);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(string source, string reason)
        {
            if (_currentRoute == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("No active Route is available."));
            }

            return _activityFlowRuntime.ClearActivityAsync(source, reason);
        }
    }
}
