using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed class RouteExitedEvent : IEvent
    {
        public RouteExitedEvent(RouteAsset route, RouteAsset nextRoute, string source, string reason)
        {
            Route = route;
            NextRoute = nextRoute;
            Source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason.Trim();
        }

        public RouteAsset Route { get; }

        public RouteAsset NextRoute { get; }

        public string Source { get; }

        public string Reason { get; }
    }
}
