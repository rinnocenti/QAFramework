using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Minimal owner for the game-flow route handoff.
    /// It accepts route requests and delegates route startup to Route Lifecycle.
    /// </summary>
    internal sealed class GameFlowRuntime
    {
        private readonly RouteLifecycleRuntime routeLifecycleRuntime = new RouteLifecycleRuntime();
        private bool routeRequestInFlight;

        internal async Task<FrameworkGameFlowStartResult> StartAsync(GameApplicationAsset gameApplication)
        {
            if (gameApplication == null)
            {
                return FrameworkGameFlowStartResult.Failed("Game Application is missing.");
            }

            var startupRoute = gameApplication.StartupRoute;
            if (startupRoute == null)
            {
                return FrameworkGameFlowStartResult.Failed("Startup Route is missing.");
            }

            if (!startupRoute.HasPrimaryScene)
            {
                return FrameworkGameFlowStartResult.Failed("Startup Route Primary Scene is missing.");
            }

            var routeLifecycleResult = await StartRouteCoreAsync(startupRoute);
            if (!routeLifecycleResult.Started)
            {
                return FrameworkGameFlowStartResult.Failed(routeLifecycleResult.Message);
            }

            return FrameworkGameFlowStartResult.StartedWith(startupRoute, routeLifecycleResult);
        }

        internal async Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            var resolvedSource = FrameworkRouteRequestResult.NormalizeSource(source);
            var resolvedReason = FrameworkRouteRequestResult.NormalizeReason(reason);

            if (targetRoute == null)
            {
                return FrameworkRouteRequestResult.FailedInvalidConfig(
                    "Route Request failed. Target Route is missing.",
                    null,
                    resolvedSource,
                    resolvedReason);
            }

            if (!targetRoute.HasPrimaryScene)
            {
                return FrameworkRouteRequestResult.FailedInvalidConfig(
                    $"Route Request failed. Target Route '{targetRoute.RouteName}' has no Primary Scene.",
                    targetRoute,
                    resolvedSource,
                    resolvedReason);
            }

            if (routeRequestInFlight)
            {
                return FrameworkRouteRequestResult.IgnoredAlreadyInFlight(targetRoute, resolvedSource, resolvedReason);
            }

            if (routeLifecycleRuntime.IsRouteActive(targetRoute))
            {
                return FrameworkRouteRequestResult.IgnoredAlreadyActive(targetRoute, resolvedSource, resolvedReason);
            }

            routeRequestInFlight = true;
            try
            {
                var routeLifecycleResult = await StartRouteCoreAsync(targetRoute);
                if (!routeLifecycleResult.Started)
                {
                    return FrameworkRouteRequestResult.FailedInvalidConfig(
                        routeLifecycleResult.Message,
                        targetRoute,
                        resolvedSource,
                        resolvedReason);
                }

                return FrameworkRouteRequestResult.SucceededWith(
                    targetRoute,
                    resolvedSource,
                    resolvedReason,
                    routeLifecycleResult);
            }
            finally
            {
                routeRequestInFlight = false;
            }
        }

        private Task<RouteLifecycleStartResult> StartRouteCoreAsync(RouteAsset route)
        {
            return routeLifecycleRuntime.StartRouteAsync(route);
        }
    }
}
