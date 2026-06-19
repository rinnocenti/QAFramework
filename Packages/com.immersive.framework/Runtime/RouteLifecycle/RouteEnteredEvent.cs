using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed class RouteEnteredEvent : IEvent
    {
        public RouteEnteredEvent(RouteAsset route, RouteAsset previousRoute)
        {
            Route = route;
            PreviousRoute = previousRoute;
        }

        public RouteAsset Route { get; }

        public RouteAsset PreviousRoute { get; }
    }
}
