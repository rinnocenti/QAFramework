using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed class RouteExitedEvent : IEvent
    {
        public RouteExitedEvent(RouteAsset route, RouteAsset nextRoute)
        {
            Route = route;
            NextRoute = nextRoute;
        }

        public RouteAsset Route { get; }

        public RouteAsset NextRoute { get; }
    }
}
