using System.Threading.Tasks;
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
        private RouteAsset currentRoute;

        internal RouteAsset CurrentRoute => currentRoute;

        internal bool IsRouteActive(RouteAsset route)
        {
            return route != null && ReferenceEquals(currentRoute, route);
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

            currentRoute = route;
            return RouteLifecycleStartResult.StartedWith(route, previousRoute, sceneLifecycleResult);
        }
    }
}
