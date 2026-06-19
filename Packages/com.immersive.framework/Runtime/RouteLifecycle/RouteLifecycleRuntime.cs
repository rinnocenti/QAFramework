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
        private readonly SceneLifecycleRuntime sceneLifecycleRuntime = new SceneLifecycleRuntime();
        private readonly ActivityFlowRuntime activityFlowRuntime = new ActivityFlowRuntime();
        private readonly EventBus<RouteEnteredEvent> routeEnteredEvents = new EventBus<RouteEnteredEvent>();
        private readonly EventBus<RouteExitedEvent> routeExitedEvents = new EventBus<RouteExitedEvent>();
        private RouteAsset currentRoute;

        internal RouteAsset CurrentRoute => currentRoute;

        internal ActivityAsset CurrentActivity => activityFlowRuntime.CurrentActivity;

        internal bool HasActiveRoute => currentRoute != null;

        internal bool HasActiveActivity => activityFlowRuntime.HasActiveActivity;

        internal bool IsRouteActive(RouteAsset route)
        {
            return route != null && ReferenceEquals(currentRoute, route);
        }

        internal IEventBinding SubscribeRouteEntered(Action<RouteEnteredEvent> handler)
        {
            return routeEnteredEvents.Subscribe(handler);
        }

        internal IEventBinding SubscribeRouteExited(Action<RouteExitedEvent> handler)
        {
            return routeExitedEvents.Subscribe(handler);
        }

        internal bool IsActivityActive(ActivityAsset activity)
        {
            return activityFlowRuntime.IsActivityActive(activity);
        }

        internal async Task<RouteLifecycleStartResult> StartRouteAsync(RouteAsset route)
        {
            if (route == null)
            {
                return RouteLifecycleStartResult.Failed("Route is missing.");
            }

            if (!route.HasPrimaryScene)
            {
                return RouteLifecycleStartResult.Failed("Route Primary Scene is missing.");
            }

            var previousRoute = currentRoute;
            var sceneLifecycleResult = await sceneLifecycleRuntime.LoadPrimarySceneAsync(route);
            if (!sceneLifecycleResult.Loaded)
            {
                return RouteLifecycleStartResult.Failed(sceneLifecycleResult.Message);
            }

            var activityFlowResult = await activityFlowRuntime.StartStartupActivityAsync(route);
            if (!activityFlowResult.Completed)
            {
                return RouteLifecycleStartResult.Failed(activityFlowResult.Message);
            }

            currentRoute = route;
            PublishRouteTransition(previousRoute, route);
            return RouteLifecycleStartResult.StartedWith(route, previousRoute, sceneLifecycleResult, activityFlowResult);
        }

        private void PublishRouteTransition(RouteAsset previousRoute, RouteAsset nextRoute)
        {
            if (previousRoute != null && !ReferenceEquals(previousRoute, nextRoute))
            {
                routeExitedEvents.Publish(new RouteExitedEvent(previousRoute, nextRoute));
            }

            if (nextRoute != null && !ReferenceEquals(previousRoute, nextRoute))
            {
                routeEnteredEvents.Publish(new RouteEnteredEvent(nextRoute, previousRoute));
            }
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(ActivityAsset activity)
        {
            if (currentRoute == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("No active Route is available."));
            }

            return activityFlowRuntime.StartActivityAsync(activity);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync()
        {
            if (currentRoute == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("No active Route is available."));
            }

            return activityFlowRuntime.ClearActivityAsync();
        }
    }
}
