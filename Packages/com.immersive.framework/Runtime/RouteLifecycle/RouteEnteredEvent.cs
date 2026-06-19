using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed class RouteEnteredEvent : IEvent
    {
        public RouteEnteredEvent(RouteAsset route, RouteAsset previousRoute, string source, string reason)
        {
            Route = route;
            PreviousRoute = previousRoute;
            Source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason.Trim();
        }

        public RouteAsset Route { get; }

        public RouteAsset PreviousRoute { get; }

        public string Source { get; }

        public string Reason { get; }
    }
}
