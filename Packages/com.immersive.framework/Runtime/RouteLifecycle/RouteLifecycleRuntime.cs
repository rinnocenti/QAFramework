using System;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Minimal owner for starting and switching Routes.
    /// It owns the active Route identity and delegates scene loading to Scene Lifecycle.
    /// </summary>
    internal sealed class RouteLifecycleRuntime
    {
        private readonly SceneLifecycleRuntime _sceneLifecycleRuntime = new SceneLifecycleRuntime();
        private readonly ActivityFlowRuntime _activityFlowRuntime = new ActivityFlowRuntime();
        private readonly RouteContentRuntime _routeContentRuntime = new RouteContentRuntime();
        private readonly EventBus<RouteEnteredEvent> _routeEnteredEvents = new EventBus<RouteEnteredEvent>();
        private readonly EventBus<RouteExitedEvent> _routeExitedEvents = new EventBus<RouteExitedEvent>();
        private RouteAsset _currentRoute;

        internal RouteAsset CurrentRoute => _currentRoute;

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

        internal async Task<RouteLifecycleStartResult> StartRouteAsync(RouteAsset route, string source, string reason)
        {
            var resolvedSource = NormalizeSource(source);
            var resolvedReason = NormalizeReason(reason);

            if (route == null)
            {
                return RouteLifecycleStartResult.Failed("Route is missing.");
            }

            if (!route.HasPrimaryScene)
            {
                return RouteLifecycleStartResult.Failed("Route Primary Scene is missing.");
            }

            var previousRoute = _currentRoute;
            var changesRoute = previousRoute == null || !ReferenceEquals(previousRoute, route);

            if (changesRoute)
            {
                _routeContentRuntime.ExitRouteContent(previousRoute, route, resolvedSource, resolvedReason);
            }

            var sceneLifecycleResult = await _sceneLifecycleRuntime.LoadPrimarySceneAsync(route);
            if (!sceneLifecycleResult.Loaded)
            {
                if (changesRoute)
                {
                    _routeContentRuntime.EnterRouteContent(previousRoute, null, resolvedSource, resolvedReason);
                }

                return RouteLifecycleStartResult.Failed(sceneLifecycleResult.Message);
            }

            if (changesRoute)
            {
                _routeContentRuntime.EnterRouteContent(route, previousRoute, resolvedSource, resolvedReason);
            }

            var activityFlowResult = await _activityFlowRuntime.StartStartupActivityAsync(route, resolvedSource, resolvedReason);
            if (!activityFlowResult.Completed)
            {
                return RouteLifecycleStartResult.Failed(activityFlowResult.Message);
            }

            _currentRoute = route;
            PublishRouteTransition(previousRoute, route, resolvedSource, resolvedReason);
            return RouteLifecycleStartResult.StartedWith(route, previousRoute, sceneLifecycleResult, activityFlowResult);
        }

        private void PublishRouteTransition(RouteAsset previousRoute, RouteAsset nextRoute, string source, string reason)
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

        internal Task<ActivityFlowStartResult> StartActivityAsync(ActivityAsset activity, string source, string reason)
        {
            var resolvedSource = NormalizeSource(source);
            var resolvedReason = NormalizeReason(reason);

            if (_currentRoute == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("No active Route is available."));
            }

            return _activityFlowRuntime.StartActivityAsync(activity, resolvedSource, resolvedReason);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(string source, string reason)
        {
            var resolvedSource = NormalizeSource(source);
            var resolvedReason = NormalizeReason(reason);

            if (_currentRoute == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("No active Route is available."));
            }

            return _activityFlowRuntime.ClearActivityAsync(resolvedSource, resolvedReason);
        }

        private static string NormalizeSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "None" : reason.Trim();
        }
    }
}
